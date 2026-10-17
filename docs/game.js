/* DUOVEX Browser Playtest v0.51
 * Charts are the unmodified JSON assets from DUOVEX Unity v0.51.
 * Reimplements the gameplay for GitHub Pages without Unity WebGL.
 */
'use strict';
(() => {
  const $ = id => document.getElementById(id);
  const screenIDs = ['menu','select','game','results','how','settings'];
  const KEYS = ['KeyD','KeyF','KeyJ','KeyK'];
  const COLORS = { cyan:'#72eaff',violet:'#b48eff',perfect:'#70f0ff',great:'#9bffa9',good:'#ffd76e',bad:'#ff9461',miss:'#ff647f'};
  const TITLES = ['PERFECT','GREAT','GOOD','BAD','MISS'];
  const WEIGHTS = [1,.8,.5,.2,0];
  const GRADES = ['#70f0ff','#9bffa9','#ffd76e','#ff9461','#ff647f'];
  const MAPS = [
    {id:1,title:'TUTORIAL',subtitle:'WHITE / BLACK',bpm:110,src:'charts/Tutorial.json',visual:'I'},
    {id:2,title:'INTERLOCK',subtitle:'SHARED / DUAL',bpm:125,src:'charts/Level2.json',visual:'II'},
    {id:3,title:'OVERDRIVE',subtitle:'HOLD / COMBO',bpm:150,src:'charts/Level3.json',visual:'III'}
  ];
  const canvas = $('gameCanvas'), ctx = canvas.getContext('2d');
  const W=1280,H=720,LX=496,LW=96,TY=68,HY=596,SCROLL=330;
  const state = {
    screen:'menu', selected:1, loaded:new Map(), speed:readNumber('duovex_speed',1,.5,3),
    volume:readNumber('duovex_volume',.32,0,1), metronome:readBool('duovex_metronome'),
    playing:false, paused:false, hold:[false,false,false,false],
    chart:null, notes:[], startMs:0, pauseMs:0, now:0, length:0, lastBeat:-1,
    earned:0, judged:0, combo:0, maxCombo:0, counts:[0,0,0,0,0], fast:0,slow:0,
    judgment:null, bursts:[], keyPulses:[0,0,0,0], artStars:[], resultHistory:[]
  };
  const fmt=(x)=>Number.isFinite(x)? x.toFixed(2):'0.00';
  function readNumber(key,def,min,max){try{const x=Number(localStorage.getItem(key));return localStorage.getItem(key)===null?def:Math.max(min,Math.min(max,x));}catch{return def;}}
  function readBool(key){try{return localStorage.getItem(key)==='yes';}catch{return false;}}
  function save(){try{localStorage.setItem('duovex_speed',String(state.speed));localStorage.setItem('duovex_volume',String(state.volume));localStorage.setItem('duovex_metronome',state.metronome?'yes':'no');}catch{}}
  function show(id){
    state.screen=id;
    for(const name of screenIDs) $('screen-'+name).classList.toggle('active',name===id);
    $('topSettings').style.visibility=id==='game'?'hidden':'visible';
    document.title=(id==='game'?`DUOVEX — LV.${state.selected}`:'DUOVEX — Play v0.51');
  }
  function gotoMenu(){cancelGame();show('menu');}
  function gotoSelect(){cancelGame();show('select');}
  function cancelGame(){state.playing=false;state.paused=false;state.hold.fill(false);$('gamePause').classList.add('hidden');}
  const audio = {context:null,buffers:[],enabled:false};
  function context(){
    try{if(!audio.context) audio.context=new (window.AudioContext||window.webkitAudioContext)();audio.context.resume();return audio.context;}catch{return null;}
  }
  async function prepareSound(){
    const c=context();if(!c)return;
    for(let n=0;n<2;n++){
      if(audio.buffers[n])continue;
      const f=n?'sfx/HitBlack.b64':'sfx/HitWhite.b64';
      try{const response=await fetch(f);if(!response.ok)throw Error(response.status);const encoded=(await response.text()).trim();const binary=atob(encoded);const bytes=new Uint8Array(binary.length);for(let i=0;i<binary.length;i++)bytes[i]=binary.charCodeAt(i);audio.buffers[n]=await c.decodeAudioData(bytes.buffer);}catch(e){console.warn('Could not load hit sound',f,e);}
    }
  }
  function sfx(input){
    if(state.volume<=0)return;
    const c=context();if(!c)return;
    const buf=audio.buffers[input<2?0:1];
    if(buf){const src=c.createBufferSource(),gain=c.createGain();src.buffer=buf;gain.gain.value=state.volume*.92;src.connect(gain).connect(c.destination);src.start();return;}
    const osc=c.createOscillator(),gain=c.createGain(),t=c.currentTime;
    osc.type='triangle';osc.frequency.setValueAtTime(input<2?183:147,t);osc.frequency.exponentialRampToValueAtTime(input<2?75:62,t+.06);
    gain.gain.setValueAtTime(state.volume*.18,t);gain.gain.exponentialRampToValueAtTime(.0001,t+.09);osc.connect(gain).connect(c.destination);osc.start(t);osc.stop(t+.10);
  }
  function beatSfx(accent){
    const c=context();if(!c)return;
    const t=c.currentTime,o=c.createOscillator(),v=c.createGain();o.type='sine';o.frequency.setValueAtTime(accent?930:660,t);v.gain.setValueAtTime(.0001,t);v.gain.linearRampToValueAtTime(state.volume*.22,t+.005);v.gain.exponentialRampToValueAtTime(.0001,t+.040);o.connect(v).connect(c.destination);o.start(t);o.stop(t+.048);
  }
  async function loadChart(level){
    const m=MAPS.find(x=>x.id===level);
    if(!m)return null;
    if(state.loaded.has(level))return state.loaded.get(level);
    const response=await fetch(m.src,{cache:'no-cache'});
    if(!response.ok)throw Error('Chart not found: '+m.src);
    const data=await response.json();
    if(!Array.isArray(data.notes)||!data.notes.length||!(data.bpm>0))throw Error('Invalid chart');
    for(const n of data.notes){if(!Number.isFinite(n.beat)||!Number.isInteger(n.input)||n.input<0||n.input>3||n.durationBeats<0)throw Error('Invalid note');}
    data.notes.sort((a,b)=>a.beat-b.beat||a.input-b.input);
    state.loaded.set(level,data);
    return data;
  }
  function populateCards(){
    const grid=$('levelGrid');grid.replaceChildren();
    MAPS.forEach(m=>{
      const chart=state.loaded.get(m.id);
      const seconds=chart?Math.max(...chart.notes.map(n=>n.beat+n.durationBeats))*60/chart.bpm:0;
      const len=seconds?`${Math.floor(seconds/60)}:${String(Math.floor(seconds%60)).padStart(2,'0')}`:'--:--';
      const count=chart?chart.notes.length:'—';
      const b=document.createElement('button');b.className='level-card';
      b.innerHTML=`<div class="level-visual"><span class="level-symbol">${m.visual}</span><span class="level-no">0${m.id}</span></div><div class="level-body"><span class="level-index">LEVEL 0${m.id} / ${m.subtitle}</span><h2>${m.title}</h2><div class="level-stats"><span>${m.bpm} BPM</span><span>${count} NOTES</span><span>${len}</span><span class="level-cta">↗</span></div></div>`;
      b.addEventListener('click',()=>start(m.id));grid.appendChild(b);
    });
  }
  function openSelect(){populateCards();show('select');}
  async function start(level){
    try{
      const chart=await loadChart(level);await prepareSound();
      state.selected=level;state.chart=chart;state.hold.fill(false);
      state.notes=chart.notes.map(n=>({ ...n,time:n.beat*60/chart.bpm+(chart.offsetMs||0)/1000,
        tail:(n.beat+n.durationBeats)*60/chart.bpm+(chart.offsetMs||0)/1000,
        headDone:false,finished:false,nextTick:1 }));
      state.length=Math.max(...state.notes.map(n=>n.tail))+1.5;
      state.combo=state.maxCombo=state.judged=state.earned=state.fast=state.slow=0;
      state.counts=[0,0,0,0,0];state.judgment=null;state.bursts=[];state.keyPulses=[0,0,0,0];state.lastBeat=-1;
      state.startMs=performance.now()+900;state.playing=true;state.paused=false;
      $('gamePause').classList.add('hidden');show('game');updateSpeedUI();render(performance.now());
    }catch(e){console.error(e);alert('레벨 파일을 불러오지 못했습니다.\n'+e.message+'\nGitHub Pages 또는 로컬 서버에서 실행해 주세요.');}
  }
  function songTime(now=performance.now()){return (now-state.startMs)/1000;}
  function pauseGame(){if(!state.playing||state.paused)return;state.paused=true;state.pauseMs=performance.now();$('gamePause').classList.remove('hidden');state.hold.fill(false);}
  function resumeGame(){if(!state.playing||!state.paused)return;state.startMs+=performance.now()-state.pauseMs;state.paused=false;$('gamePause').classList.add('hidden');}
  function adjustSpeed(value){state.speed=Math.round(Math.min(3,Math.max(.5,value))*10)/10;save();updateSpeedUI();}
  function updateSpeedUI(){$('speedValue').textContent='SPEED '+state.speed.toFixed(1)+'×';$('speedRange').value=state.speed;$('speedLabel').textContent=state.speed.toFixed(1)+'×';}
  function judge(delta){const a=Math.abs(delta*1000);return a<=50?0:a<=80?1:a<=120?2:a<=160?3:4;}
  function register(index,delta,key,tick,time){
    state.judged++;state.earned+=WEIGHTS[index];state.counts[index]++;
    if(index===4)state.combo=0;
    else {state.combo++;state.maxCombo=Math.max(state.maxCombo,state.combo);state.bursts.push({key,at:time,tick});if(!tick)sfx(key);}
    if(tick&&index!==4)return;
    const timing=(!tick&&index!==0&&index!==4)?(delta<-.0005?'FAST':delta>.0005?'SLOW':''):'';
    if(timing==='FAST')state.fast++;else if(timing==='SLOW')state.slow++;
    state.judgment={index,timing,at:time};
  }
  function keyDown(index){
    if(!state.playing||state.paused)return;
    if(state.hold[index])return;
    state.hold[index]=true;state.keyPulses[index]=performance.now();
    let closest=null,best=.16001;const now=songTime();
    for(const n of state.notes){if(n.input!==index||n.headDone)continue;const distance=Math.abs(now-n.time);if(distance<best){best=distance;closest=n;}}
    if(closest){closest.headDone=true;const d=now-closest.time;register(judge(d),d,index,false,performance.now());if(closest.durationBeats<=0)closest.finished=true;}
  }
  function keyUp(index){state.hold[index]=false;}
  function gameTick(now){
    if(!state.playing||state.paused)return;
    const time=songTime(now);state.now=time;
    if(state.metronome&&time>=0){const beat=Math.floor(time*state.chart.bpm/60);if(beat!==state.lastBeat){state.lastBeat=beat;beatSfx(beat%4===0);}}
    for(const n of state.notes){
      if(!n.headDone&&time>n.time+.160){n.headDone=true;register(4,0,n.input,false,now);if(n.durationBeats<=0)n.finished=true;}
      if(n.durationBeats<=0)continue;
      const count=Math.ceil(n.durationBeats*state.chart.tickDivision-.00001);
      while(n.nextTick<=count){
        const tickTime=Math.min(n.beat+n.nextTick/state.chart.tickDivision,n.beat+n.durationBeats)*60/state.chart.bpm+(state.chart.offsetMs||0)/1000;
        if(time<tickTime)break;
        register(state.hold[n.input]&&n.headDone?0:4,0,n.input,true,now);n.nextTick++;
      }
      if(n.nextTick>count&&time>=n.tail)n.finished=true;
    }
    state.bursts=state.bursts.filter(b=>now-b.at<290);
    if(time>=state.length){finish();return;}
  }
  function finish(){if(!state.playing)return;state.playing=false;state.hold.fill(false);showResults();}
  function showResults(){
    const chart=state.chart,$set=(id,value)=>$(id).textContent=value;
    $set('resultTitle',chart.title.replace('LEVEL ','LV. '));
    $set('resultAcc',fmt(state.judged?state.earned/state.judged*100:100)+'%');
    $set('resultScore',Math.round(state.earned*1000).toLocaleString('en-US'));
    $set('resultMax','MAX COMBO '+state.maxCombo);
    $set('resultFastSlow','FAST '+state.fast+' / SLOW '+state.slow);
    $('resultGrades').innerHTML=TITLES.map((name,i)=>`<div class="grade-box"><span class="name" style="color:${GRADES[i]}">${name}</span><b>${state.counts[i]}</b></div>`).join('');
    show('results');
  }
  function setupCanvas(){
    const dpr=Math.max(1,Math.min(2,window.devicePixelRatio||1));
    canvas.width=W*dpr;canvas.height=H*dpr;
    ctx.setTransform(dpr,0,0,dpr,0,0);
  }
  function rect(x,y,w,h,color){ctx.fillStyle=color;ctx.fillRect(x,y,w,h);}
  function stroke(x,y,w,h,color,width=1){ctx.strokeStyle=color;ctx.lineWidth=width;ctx.strokeRect(x,y,w,h);}
  function text(value,x,y,size,color,weight=700,align='left',font='Inter, system-ui, sans-serif'){
    ctx.font=`${weight} ${size}px ${font}`;ctx.textAlign=align;ctx.textBaseline='middle';ctx.fillStyle=color;ctx.fillText(value,x,y);
  }
  function glowLine(x1,y1,x2,y2,color,blur,w){ctx.save();ctx.strokeStyle=color;ctx.lineWidth=w;ctx.shadowColor=color;ctx.shadowBlur=blur;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();ctx.restore();}
  function noteY(time){return HY-(time-state.now)*SCROLL*state.speed;}
  function physical(input){return input===0?0:input===3?2:1;}
  function drawBackground(now){
    const bg=ctx.createLinearGradient(0,0,W,H);bg.addColorStop(0,'#0b1020');bg.addColorStop(.56,'#0b1223');bg.addColorStop(1,'#10132a');rect(0,0,W,H,bg);
    // Soft ambience remains outside the playable lanes.
    ctx.save();ctx.globalAlpha=.3;
    for(const o of [[290,240,220,'#234967'],[1040,510,280,'#49265c']]){
      const g=ctx.createRadialGradient(o[0],o[1],0,o[0],o[1],o[2]);g.addColorStop(0,o[3]);g.addColorStop(1,'#0b102000');rect(o[0]-o[2],o[1]-o[2],o[2]*2,o[2]*2,g);
    }
    ctx.restore();
    ctx.globalAlpha=.25;
    const tick=(now/90)%120;
    for(let i=0;i<27;i++){
      const x=(i*197+37)%W,y=((i*109+tick*(i%3+1))%(H+80))-40;
      rect(x,y,1,9+(i%4)*8,i%2?'#6577b2':'#507c8d');
    }
    ctx.globalAlpha=1;
  }
  function drawGear(){
    const g=ctx.createLinearGradient(LX,TY,LX,HY);
    g.addColorStop(0,'#0c142650');g.addColorStop(.6,'#152139ec');g.addColorStop(1,'#1d2a45');
    rect(LX,TY,LW*3,HY-TY,g);
    rect(LX+LW,TY,LW,HY-TY,'#9282ff12');
    rect(LX,TY,LW,HY-TY,'#63ebff08');rect(LX+LW*2,TY,LW,HY-TY,'#a68cff09');
    const fog=ctx.createLinearGradient(0,TY,0,TY+125);fog.addColorStop(0,'#0c1227');fog.addColorStop(1,'#0c122700');rect(LX,TY,LW*3,125,fog);
    ctx.save();ctx.beginPath();ctx.rect(LX,TY,LW*3,HY-TY+2);ctx.clip();
    for(let i=0;i<3;i++){
      const ax=LX+i*LW;
      const cc=i===2?'#a68aff':i===1?'#b0b8ff':'#69e6ff';
      const grad=ctx.createLinearGradient(ax,0,ax+LW,0);grad.addColorStop(0,'#00000000');grad.addColorStop(.5,cc+'07');grad.addColorStop(1,'#00000000');rect(ax,TY,LW,HY-TY,grad);
    }
    ctx.restore();
    // The player requested no distracting vertical judgement markers.
  }
  function drawTap(x,y,w,kind,combined){
    if(y<TY-28||y>HY+19)return;
    const accent=combined?'#bfb0ff':kind==='white'?'#71eaff':'#b891ff';
    ctx.save();ctx.shadowColor=accent;ctx.shadowBlur=12;rect(x,y,w,17,combined?'#f9fbff':kind==='white'?'#f5faff':'#0a0d17');ctx.restore();
    if(combined){
      ctx.save();ctx.beginPath();ctx.moveTo(x,y);ctx.lineTo(x+w,y);ctx.lineTo(x,y+17);ctx.closePath();ctx.fillStyle='#101421';ctx.fill();ctx.restore();
    }
    stroke(x,y,w,17,accent,1.8);rect(x,y-2,w,2,accent);rect(x+4,y+19,w-8,2,accent+'9a');
  }
  function drawHold(x,headY,tailY,w,white,active){
    const top=Math.max(TY,tailY),bottom=Math.min(HY+1,headY);
    const color=white?COLORS.cyan:COLORS.violet;
    if(bottom>top){rect(x,top,w,bottom-top,white?(active?'#76eaff66':'#70d9ff37'):(active?'#ae8cff66':'#ae83ff37'));
      rect(x,top,2,bottom-top,color);rect(x+w-2,top,2,bottom-top,color);
      for(let yy=top+14;yy<bottom;yy+=24)rect(x+5,yy,w-10,1,color+'55');
    }
  }
  function findPair(n,onlyHold){
    if(n.input!==1&&n.input!==2)return null;
    return state.notes.find(m=>m!==n&&m.input===(n.input===1?2:1)&&
      Math.abs(m.beat-n.beat)<.00001&&Math.abs(m.durationBeats-n.durationBeats)<.00001&&
      (!!m.durationBeats)===onlyHold);
  }
  function drawNotes(){
    ctx.save();ctx.beginPath();ctx.rect(LX,TY,LW*3,HY-TY+3);ctx.clip();
    for(const n of state.notes){
      if(n.finished||n.durationBeats<=0)continue;
      const head=noteY(n.time),tail=noteY(n.tail);if(head<TY-50||tail>HY+25)continue;
      let x=LX+physical(n.input)*LW+6,w=LW-12;
      if((n.input===1||n.input===2)&&findPair(n,true)){w=(LW-12)/2;if(n.input===2)x+=w;}
      drawHold(x,head,tail,w,n.input<2,state.hold[n.input]&&n.headDone);
    }
    for(const n of state.notes){
      if(n.finished||n.headDone||n.durationBeats<=0||n.input!==1||!findPair(n,true))continue;
      drawTap(LX+LW+6,noteY(n.time),LW-12,'white',true);
    }
    for(const n of state.notes){
      if(n.finished||n.headDone||n.durationBeats>0)continue;
      if(n.input===2&&findPair(n,false)&&!findPair(n,false).headDone)continue;
      const other=findPair(n,false);
      if(n.input===1&&other&&!other.headDone){drawTap(LX+LW+6,noteY(n.time),LW-12,'white',true);continue;}
      let x=LX+physical(n.input)*LW+6,w=LW-12;
      // If the other half of a DUAL was hit first, show the unjudged half.
      if(other&&other.headDone){w=(LW-12)/2;if(n.input===2)x+=w;}
      drawTap(x,noteY(n.time),w,n.input<2?'white':'black',false);
    }
    ctx.restore();
  }
  function drawLine(){
    const x=LX-6,w=LW*3+12;
    ctx.save();ctx.shadowBlur=26;ctx.shadowColor='#75eaff';rect(x-8,HY-4,w+16,8,'#66dffe3a');ctx.restore();
    rect(x,HY-6,w,2,COLORS.cyan);rect(x-1,HY-3,w+2,6,'#f5fbff');rect(x,HY+4,w,2,COLORS.violet);
  }
  function drawKeyAreas(now){
    for(let i=0;i<4;i++){
      let x=LX+physical(i)*LW,w=LW;
      if(i===1){x=LX+LW;w=LW/2;}if(i===2){x=LX+LW+LW/2;w=LW/2;}
      const color=i<2?COLORS.cyan:COLORS.violet;
      const pressed=state.hold[i]||now-state.keyPulses[i]<85;
      if(pressed){ctx.save();ctx.shadowBlur=20;ctx.shadowColor=color;rect(x+2,HY+8,w-4,33,i<2?'#55dcff60':'#9a7bfa75');ctx.restore();}
      rect(x+3,HY+9,w-6,31,pressed?(i<2?'#286278':'#584781'):'#16233a');
      stroke(x+3,HY+9,w-6,31,pressed?color:'#44516d',pressed?1.7:1);
      text('DFJK'[i],x+w/2,HY+25,16,pressed?'#fff':'#b5c9e2',800,'center');
    }
  }
  function drawBursts(now){
    for(const b of state.bursts){
      const age=(now-b.at)/270;if(age<0||age>1)continue;
      let x=LX+physical(b.key)*LW,w=LW;
      if(b.key===1){x+=LW/4;w/=2;}if(b.key===2){x+=LW*3/4;w/=2;}
      const c=b.key<2?COLORS.cyan:COLORS.violet;
      ctx.save();ctx.globalAlpha=(1-age)*.85;ctx.strokeStyle=c;ctx.lineWidth=2.5*(1-age)+.5;ctx.shadowColor=c;ctx.shadowBlur=15;
      ctx.beginPath();ctx.ellipse(x+w/2,HY-6-age*25,w*(.4+age*.45),8+age*24,0,0,Math.PI*2);ctx.stroke();ctx.restore();
    }
  }
  function drawHUD(now){
    const acc=state.judged?state.earned/state.judged*100:100;
    text('DUOVEX',53,57,21,'#d7ecff',800);
    rect(53,80,64,3,COLORS.cyan);
    text(state.chart.title,53,118,15,'#91aaca',700);
    text('ACCURACY',53,196,14,'#8397b7',700);
    text(fmt(acc)+'%',53,242,43,'#f2fbff',800);
    text('COMBO',830,181,16,'#93a6c4',700);
    text(String(state.combo).padStart(4,'0'),830,237,57,'#f0f9ff',800);
    text('SCORE',830,293,13,'#7d94b5',700);
    text(Math.round(state.earned*1000).toLocaleString('en-US'),900,294,18,'#c7ddf2',800);
    text('SPEED '+state.speed.toFixed(1)+'×',830,328,15,'#8bb2d5',700);
    if(state.selected===1){
      const bt=state.now*state.chart.bpm/60;
      const segments=[{from:0,t:'WHITE NOTES',k:'D / F'},{from:18,t:'BLACK NOTES',k:'J / K'},{from:34,t:'SHARED LANE',k:'F / J'},{from:50,t:'DUAL INPUT',k:'F + J'},{from:67,t:'HOLD / TICKS',k:'HOLD'},{from:85,t:'FINAL PRACTICE',k:'D F J K'}];
      let step=0;for(let i=0;i<segments.length;i++)if(bt>=segments[i].from)step=i;
      rect(48,462,396,115,'#101b30da');stroke(48,462,396,115,'#52688688');rect(48,462,396,3,step===1?COLORS.violet:COLORS.cyan);
      text('STEP '+String(step+1).padStart(2,'0')+' / 06',64,488,12,'#92abc8',800);
      text(segments[step].t,64,519,20,'#eaf8ff',800);
      text(segments[step].k,64,544,17,'#7fe8ff',700);
      for(let i=0;i<6;i++)rect(64+61*i,560,54,4,i<=step?COLORS.cyan:'#2b3a52');
    }
    const j=state.judgment;
    if(j&&now-j.at<540){
      const t=(now-j.at)/540,alpha=Math.min(1,(1-t)*1.6);
      ctx.save();ctx.globalAlpha=alpha;
      text(TITLES[j.index],LX+LW*1.5,441,34,GRADES[j.index],900,'center');
      if(j.timing==='FAST')text('FAST',LX+LW*1.5,410,14,'#bbdcff',800,'center');
      if(j.timing==='SLOW')text('SLOW',LX+LW*1.5,470,14,'#ffc1d1',800,'center');
      ctx.restore();
    }
    const pos=Math.min(1,Math.max(0,state.now/state.length));
    rect(53,650,1174,3,'#2d3b55');rect(53,650,1174*pos,3,COLORS.cyan);
    text(Math.max(0,state.now).toFixed(1)+' / '+state.length.toFixed(1)+' s',1227,675,13,'#788fac',600,'right');
    if(state.now<0){ctx.save();ctx.globalAlpha=.7;text('READY',LX+LW*1.5,340,45,'#e2f5ff',900,'center');ctx.restore();}
  }
  function render(now){
    if(state.screen!=='game')return;
    if(state.playing&&!state.paused)gameTick(now);
    if(state.screen!=='game')return;
    state.now=songTime(state.paused?state.pauseMs:now);
    drawBackground(now);drawGear();drawNotes();drawLine();drawBursts(state.paused?state.pauseMs:now);drawKeyAreas(now);drawHUD(now);
  }
  function frame(now){if(state.screen==='game')render(now);requestAnimationFrame(frame);}
  function handleKeyDown(e){
    if(e.code==='Escape'){if(state.screen==='game'){e.preventDefault();gotoSelect();}else if(state.screen!=='menu')gotoMenu();return;}
    if(state.screen==='game'){
      if(e.code==='Space'){e.preventDefault();state.paused?resumeGame():pauseGame();return;}
      if(e.code==='F3'||e.code==='F4'){e.preventDefault();adjustSpeed(state.speed+(e.code==='F3'?-.1:.1));return;}
      if(state.paused){if(e.code==='Enter')resumeGame();return;}
      const key=KEYS.indexOf(e.code);if(key>=0){e.preventDefault();if(!e.repeat)keyDown(key);return;}
    }
    if(state.screen==='select'&&e.code==='Enter')start(state.selected);
    if(state.screen==='results'&&e.code==='Enter')start(state.selected);
  }
  function handleKeyUp(e){const key=KEYS.indexOf(e.code);if(key>=0){keyUp(key);e.preventDefault();}}
  function bind(){
    $('logo').onclick=gotoMenu;$('playButton').onclick=openSelect;$('howButton').onclick=()=>show('how');$('howBack').onclick=gotoMenu;$('howPlay').onclick=openSelect;
    $('selectBack').onclick=gotoMenu;$('selectBtn').onclick=gotoSelect;$('retryBtn').onclick=()=>start(state.selected);
    $('topSettings').onclick=()=>show('settings');$('settingsBack').onclick=gotoMenu;
    $('resumeBtn').onclick=resumeGame;$('retryPauseBtn').onclick=()=>start(state.selected);$('quitPauseBtn').onclick=gotoSelect;
    $('speedMinus').onclick=()=>adjustSpeed(state.speed-.1);$('speedPlus').onclick=()=>adjustSpeed(state.speed+.1);
    $('speedRange').oninput=e=>adjustSpeed(Number(e.target.value));$('volumeRange').oninput=e=>{state.volume=Number(e.target.value);save();$('volumeLabel').textContent=Math.round(state.volume*100)+'%';};
    $('metronomeBox').onchange=e=>{state.metronome=e.target.checked;save();};
    $('previewWhite').onclick=()=>{prepareSound();sfx(0);};$('previewBlack').onclick=()=>{prepareSound();sfx(3);};
    $('volumeRange').value=state.volume;$('volumeLabel').textContent=Math.round(state.volume*100)+'%';$('metronomeBox').checked=state.metronome;updateSpeedUI();
    document.addEventListener('keydown',handleKeyDown);document.addEventListener('keyup',handleKeyUp);
    window.addEventListener('blur',()=>{if(state.screen==='game'&&state.playing&&!state.paused)pauseGame();state.hold.fill(false);});
    document.addEventListener('visibilitychange',()=>{if(document.hidden&&state.screen==='game'&&state.playing&&!state.paused)pauseGame();});
    for(const b of document.querySelectorAll('.touch-key')){
      const i=Number(b.dataset.key);
      b.addEventListener('pointerdown',e=>{e.preventDefault();b.setPointerCapture?.(e.pointerId);keyDown(i);b.classList.add('pressed');});
      for(const name of ['pointerup','pointercancel','lostpointercapture'])b.addEventListener(name,e=>{e.preventDefault();keyUp(i);b.classList.remove('pressed');});
    }
  }
  async function init(){
    setupCanvas();bind();requestAnimationFrame(frame);
    const loaded=await Promise.allSettled(MAPS.map(m=>loadChart(m.id)));for(const x of loaded)if(x.status==='rejected')console.warn('Chart load failed',x.reason);
    populateCards();
  }
  init();
})();