using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DualSync
{
    // Attached to DUOVEX_UI. Static screens are serialized to Unity scene; editable via Inspector.
    public sealed class DuovexCanvasView : MonoBehaviour
    {
        public DuovexTheme theme;
        Dictionary<string, Component> objects;
        public T Get<T>(string id) where T:Component
        {
            if (objects==null)
            {
                objects=new Dictionary<string, Component>();
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    if (!objects.ContainsKey(t.name)) objects.Add(t.name,t);
            }
            if (!objects.TryGetValue(id,out Component c)) return null;
            return c.GetComponent<T>();
        }
        public GameObject Obj(string id) { var t=Get<Transform>(id);return t!=null?t.gameObject:null; }
        public void SetText(string id,string value)
        {
            Text t=Get<Text>(id); if(t!=null && t.text!=value) t.text=value;
        }
        public void SetColor(string id,Color color)
        {
            Image i=Get<Image>(id); if(i!=null) i.color=color;
        }
    }

    public sealed partial class DualSyncGame
    {
        DuovexCanvasView visual;
        Page lastVisualPage=(Page)(-1);
        InspectorTab lastVisualTab=(InspectorTab)(-1);
        readonly List<Image> notePool = new List<Image>();
        readonly List<PlayNote> editorRenderNotes = new List<PlayNote>();
        readonly List<Image> wavePool = new List<Image>();
        int noteCursor;
        bool visualReady;
        bool mainMotionReady;
        Vector2 mainScanBase, mainGlyphBase;
        float mainOrbitRotation, mainHaloAlpha;
        static readonly string[] tutorialLabels={"WHITE   D / F","BLACK   J / K","SHARED   F / J",
            "DUAL   F + J","HOLD","D F J K"};
        static readonly int[] editorSnaps={1,2,4,8,16,32};
        static readonly Tool[] editorTools={Tool.Select,Tool.WhiteTap,Tool.BlackTap,Tool.DualTap,
            Tool.WhiteHold,Tool.BlackHold,Tool.DualHold};

        void Start()
        {
            visual=FindAnyObjectByType<DuovexCanvasView>();
            if (visual==null) visual=DuovexUIFactory.Build();
            WireVisualButtons();
            visualReady=true;
        }

        void LateUpdate()
        {
            if (!visualReady || visual==null) return;
            if(page!=lastVisualPage)
            {
                lastVisualPage=page;
                string[] pages={"Main","Select","Game","Editor","Result","Options","Browser"};
                for(int i=0;i<pages.Length;i++)
                {
                    GameObject obj=visual.Obj("Page_"+pages[i]);
                    if(obj!=null) obj.SetActive(i==(int)page);
                }
            }
            if(page==Page.MainMenu) AnimateMainMenu();
            else if(page==Page.SongSelect) RefreshSelectVisual();
            else if(page==Page.FileBrowser) RefreshBrowserVisual();
            else if(page==Page.Options) RefreshOptionsVisual();
            else if(page==Page.Results) RefreshResultsVisual();
            else if(page==Page.Gameplay) RefreshGameplayVisual();
            else if(page==Page.Editor) RefreshEditorVisual();
        }

        void Listen(string id,Action action)
        {
            Button b=visual.Get<Button>(id);
            if(b!=null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(()=>action()); }
        }
        void WireVisualButtons()
        {
            Listen("B_Main_Play",()=>OpenSongSelect(false));
            Listen("B_Main_Edit",()=>OpenSongSelect(true));
            Listen("B_Main_Options",OpenOptions);
            for(int i=0;i<7;i++)
            {
                int index=i;
                Listen("B_Song_"+i,()=>songSelection=libraryPage+index);
                Listen("B_Browser_Row_"+i,()=>
                {
                    int idx=browserOffset+index;
                    if(idx<browserEntries.Count)
                    {
                        string target=browserEntries[idx];
                        if(Directory.Exists(target)) MoveBrowserTo(target);
                        else browserPath=target;
                    }
                });
            }
            Listen("B_Select_Up",()=>libraryPage=Mathf.Max(0,libraryPage-1));
            Listen("B_Select_Down",()=>libraryPage++);
            Listen("B_Select_Action",()=>SelectLibraryAction(songSelectForEditing));
            Listen("B_Select_Add",StartFileBrowser);
            Listen("B_Select_Back",OpenMainMenu);
            Listen("B_Options_Back",OpenMainMenu);
            Listen("B_White_Preview",()=>PreviewHitSound(true));
            Listen("B_Black_Preview",()=>PreviewHitSound(false));
            Listen("B_Speed_Minus",()=>AdjustNoteSpeed(-.1f));
            Listen("B_Speed_Plus",()=>AdjustNoteSpeed(.1f));
            Slider slider=visual.Get<Slider>("Slider_SFX");
            if(slider!=null) { slider.SetValueWithoutNotify(hitSoundVolume); slider.onValueChanged.AddListener(SetHitSoundVolume); }
            Listen("B_Result_Retry",RetryTrack);
            Listen("B_Result_Select",OpenSongSelect);
            Listen("B_Result_Main",OpenMainMenu);
            Listen("B_Browser_Cancel",()=>page=Page.SongSelect);
            Listen("B_Browser_Go",()=>
            {
                var field=visual.Get<InputField>("Input_BrowserPath");
                if(field!=null) browserPath=field.text;
                if(File.Exists(browserPath)) ImportBrowserFile(browserPath);
                else MoveBrowserTo(browserPath);
            });
            Listen("B_Browser_Up",()=> { var parent=Path.GetDirectoryName(browserFolder);if(!string.IsNullOrEmpty(parent)) MoveBrowserTo(parent);});
            Listen("B_Browser_Music",()=>MoveBrowserTo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)));
            Listen("B_Browser_Downloads",()=>MoveBrowserTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")));
            Listen("B_Browser_Home",()=>MoveBrowserTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
            Listen("B_Browser_Prev",()=>browserOffset=Mathf.Max(0,browserOffset-7));
            Listen("B_Browser_Next",()=>browserOffset=Mathf.Min(Mathf.Max(0,browserEntries.Count-7),browserOffset+7));
            Listen("B_Browser_Import",()=>{
                var field=visual.Get<InputField>("Input_BrowserPath");
                if(field!=null) browserPath=field.text;
                ImportBrowserFile(browserPath);
            });
            Listen("B_Editor_Play",()=> { if(playing) PausePlaying(); else BeginPlaying(songCursor); });
            Listen("B_Editor_Test",()=>{ PausePlaying(); editableChart=chart; tutorialActive=false;page=Page.Gameplay; Restart(true); });
            Listen("B_Editor_Save",SaveChart);
            Listen("B_Editor_Rewind",()=>Restart(false));
            Listen("B_Editor_Exit",OpenMainMenu);
            for(int i=0;i<editorTools.Length;i++) { Tool tool=editorTools[i]; Listen("B_Tool_"+i,()=>currentTool=tool); }
            for(int i=0;i<editorSnaps.Length;i++) { int division=editorSnaps[i]; Listen("B_Snap_"+i,()=>snapDivision=division); }
            for(int i=0;i<3;i++) { InspectorTab tab=(InspectorTab)i; Listen("B_Tab_"+i,()=>inspectorTab=tab); }
            Listen("B_Loop_In",()=>loopStartBeat=Mathf.Max(0,Snap(chart.SecondsToBeat(SongTime))));
            Listen("B_Loop_Out",()=>loopEndBeat=Mathf.Max(0,Snap(chart.SecondsToBeat(SongTime))));
            Listen("B_Loop_Toggle",()=>
            {
                loopEnabled=!loopEnabled;
                if(loopEndBeat<=loopStartBeat)loopEndBeat=loopStartBeat+4f;
                if(loopEnabled)SeekEditorBeat(loopStartBeat);
            });
            Listen("B_BPM_Down",()=>{Snapshot();chart.bpm=ClampBpm(chart.bpm-1f);});
            Listen("B_BPM_Up",()=>{Snapshot();chart.bpm=ClampBpm(chart.bpm+1f);});
            Listen("B_Offset_Down",()=>{Snapshot();chart.offsetMs-=10f;});
            Listen("B_Offset_Up",()=>{Snapshot();chart.offsetMs+=10f;});
            Listen("B_Zoom_Down",()=>ChangeEditorZoom(1f/1.3f,366f));
            Listen("B_Zoom_Up",()=>ChangeEditorZoom(1.3f,366f));
            Listen("B_Edit_Undo",UndoEdit); Listen("B_Edit_Redo",RedoEdit);
            Listen("B_Edit_Copy",CopySelection); Listen("B_Edit_Paste",PasteSelection);
            Listen("B_Edit_Delete",DeleteSelection); Listen("B_Edit_Load",LoadChart);
            Listen("B_Edit_Clear",()=>
            {
                if(!confirmClear) {confirmClear=true;return;}
                PausePlaying();Snapshot();chart.notes.Clear();selectedIds.Clear();BuildStates();songCursor=0f;confirmClear=false;
            });
            Listen("B_Edit_Demo",()=>
            {
                PausePlaying();Snapshot();chart=ChartStorage.Sample();editableChart=chart;
                selectedIds.Clear();snapDivision=chart.tickDivision;BuildStates();songCursor=0f;confirmClear=false;
            });
        }

        void AnimateMainMenu()
        {
            RectTransform orbit=visual.Get<RectTransform>("Main_Orbital");
            RectTransform glyph=visual.Get<RectTransform>("Main_Glyph");
            RectTransform scan=visual.Get<RectTransform>("Main_Scan");
            Image halo=visual.Get<Image>("Main_Halo");
            if(!mainMotionReady)
            {
                // Inspector changes are the animation starting positions: never write
                // hard-coded coordinates over a user-edited scene layout.
                mainGlyphBase=glyph!=null?glyph.anchoredPosition:Vector2.zero;
                mainScanBase=scan!=null?scan.anchoredPosition:Vector2.zero;
                mainOrbitRotation=orbit!=null?orbit.localEulerAngles.z:0f;
                mainHaloAlpha=halo!=null?halo.color.a:0f;
                mainMotionReady=true;
            }
            float time=Time.unscaledTime;
            if(orbit!=null) orbit.localEulerAngles=new Vector3(0,0,mainOrbitRotation+Mathf.Sin(time*.18f)*3.5f);
            if(halo!=null)
            {
                Color c=halo.color;c.a=mainHaloAlpha*(.82f+.18f*Mathf.Sin(time*.85f));halo.color=c;
            }
            if(glyph!=null) glyph.anchoredPosition=mainGlyphBase+new Vector2(0,5f*Mathf.Sin(time*.64f));
            if(scan!=null) scan.anchoredPosition=mainScanBase+new Vector2(0,-((time*78f)%367f));
        }
        void RefreshSelectVisual()
        {
            int total=importedSongs.Count+ImportedStartIndex;
            const int visible=7;
            songSelection=Mathf.Clamp(songSelection,0,Mathf.Max(0,total-1));
            libraryPage=Mathf.Clamp(libraryPage,0,Mathf.Max(0,total-visible));
            if(songSelection<libraryPage) libraryPage=songSelection;
            if(songSelection>=libraryPage+visible)libraryPage=songSelection-visible+1;
            for(int i=0;i<7;i++)
            {
                int index=libraryPage+i;
                Button b=visual.Get<Button>("B_Song_"+i);
                if(b==null)continue;
                bool valid=index<total;b.gameObject.SetActive(valid);
                if(!valid)continue;
                string name=index==0?"LV.1   TUTORIAL":index==1?"LV.2   INTERLOCK":
                    index==2?"LV.3   OVERDRIVE":index==LocalChartIndex?"LOCAL CHART":
                    importedSongs[index-ImportedStartIndex].title;
                visual.SetText("B_Song_"+i+"_Label",name);
                visual.SetColor("B_Song_"+i,index==songSelection?
                    new Color(.56f,.87f,1f):new Color(.42f,.55f,.73f));
                visual.SetColor("Select_Accent_"+i,index==songSelection?
                    new Color(.38f,.93f,1f,1f):new Color(.38f,.93f,1f,0f));
            }
            string title=songSelection==0?"LEVEL 1 / TUTORIAL":songSelection==1?"LEVEL 2 / INTERLOCK":
                songSelection==2?"LEVEL 3 / OVERDRIVE":songSelection==LocalChartIndex?"LOCAL CHART":
                importedSongs[Mathf.Clamp(songSelection-ImportedStartIndex,0,Mathf.Max(0,importedSongs.Count-1))].title;
            visual.SetText("Select_Header",songSelectForEditing?"CHART STUDIO":"SELECT TRACK");
            visual.SetText("Select_Title",title);
            visual.SetText("Select_Meta",songSelection==0?"BPM 110  /  SILENT":songSelection==1?"BPM 125  /  SILENT":
                songSelection==2?"BPM 150  /  SILENT":songSelection==LocalChartIndex?"LOCAL CHART":"IMPORTED TRACK");
            bool canOpen=!loadingAudio&&!(songSelectForEditing&&songSelection<BuiltinLevelCount);
            Button action=visual.Get<Button>("B_Select_Action");
            if(action!=null)action.interactable=canOpen;
            visual.SetText("B_Select_Action_Label",songSelectForEditing?"EDIT CHART":"PLAY");
            visual.SetText("Select_Status",loadingAudio?loadingText:(Time.unscaledTime<statusVisibleUntil?status:""));
        }
        void RefreshBrowserVisual()
        {
            var field=visual.Get<InputField>("Input_BrowserPath");
            if(field!=null&&!field.isFocused&&field.text!=browserPath) field.SetTextWithoutNotify(browserPath);
            browserOffset=Mathf.Clamp(browserOffset,0,Mathf.Max(0,browserEntries.Count-7));
            for(int i=0;i<7;i++)
            {
                int idx=i+browserOffset;
                Button b=visual.Get<Button>("B_Browser_Row_"+i);
                if(b==null)continue;
                b.gameObject.SetActive(idx<browserEntries.Count);
                if(idx>=browserEntries.Count)continue;
                string value=browserEntries[idx];
                string file=Path.GetFileName(value);
                if(file.Length>68)file=file.Substring(0,64)+"...";
                visual.SetText("B_Browser_Row_"+i+"_Label",(Directory.Exists(value)?"/  ":"   ")+file);
            }
            visual.SetText("Browser_Error",browserError);
        }
        void RefreshOptionsVisual()
        {
            visual.SetText("Options_Volume",Mathf.RoundToInt(hitSoundVolume*100f)+"%");
            visual.SetText("Options_SpeedValue",noteSpeed.ToString("0.0")+"x");
        }
        void RefreshResultsVisual()
        {
            visual.SetText("Result_Heading",tutorialActive?"LEVEL "+tutorialLevel+" COMPLETE":"TRACK COMPLETE");
            visual.SetText("Result_Accuracy",(totalJudged>0?earnedPoints/totalJudged*100f:0f).ToString("0.00")+"%");
            visual.SetText("Result_Summary","MAX COMBO  "+maxCombo+"\nMISS  "+missCount+"\nSCORE  "+Mathf.RoundToInt(earnedPoints*1000f));
            visual.SetText("Result_Breakdown","PERFECT "+resultCounts[0]+"   GREAT "+resultCounts[1]+"   GOOD "+resultCounts[2]+
                "   BAD "+resultCounts[3]+"   MISS "+resultCounts[4]+"\nFAST "+fastCount+"   SLOW "+slowCount);
        }

        void RefreshGameplayVisual()
        {
            float acc=totalJudged==0?100f:earnedPoints/totalJudged*100f;
            visual.SetText("Game_Track",chart.title);
            visual.SetText("Game_Accuracy",acc.ToString("00.00")+"%");
            visual.SetText("Game_Combo",combo.ToString("0000"));
            visual.SetText("Game_Score","SCORE  "+Mathf.RoundToInt(earnedPoints*1000f));
            visual.SetText("Game_Speed","SPEED  "+noteSpeed.ToString("0.0")+"x");
            bool show=Time.unscaledTime-lastResultTime<.52f;
            visual.SetText("Game_Judge",show?lastResult:"");
            var judge=visual.Get<Text>("Game_Judge");if(judge!=null)judge.color=lastResultColor;
            visual.SetText("Game_Fast",show&&lastTiming=="FAST"?"FAST":"");
            visual.SetText("Game_Slow",show&&lastTiming=="SLOW"?"SLOW":"");
            if(tutorialActive&&tutorialLevel==1)
            {
                int stage=TutorialStage(chart.SecondsToBeat(Mathf.Max(0,SongTime)));
                visual.SetText("Game_Guide","STEP "+(stage+1).ToString("00")+" / 06\n"+tutorialLabels[stage]);
            }
            else visual.SetText("Game_Guide","");
            float duration=CurrentSong!=null?CurrentSong.length:ChartEndTime();
            visual.SetText("Game_Time",SongTime.ToString("0.0")+" / "+duration.ToString("0.0")+"s");
            RectTransform progress=visual.Get<RectTransform>("Game_Progress");
            if(progress!=null) {Vector2 sz=progress.sizeDelta;sz.x=1178f*Mathf.Clamp01(SongTime/Mathf.Max(.01f,duration));progress.sizeDelta=sz;}
            GameObject pause=visual.Obj("Game_PausePanel");if(pause!=null)pause.SetActive(!playing);
            var paused=visual.Obj("Game_Paused");if(paused!=null)paused.SetActive(!playing);
            for(int i=0;i<4;i++)
            {
                bool guide=tutorialActive&&tutorialLevel==1&&TutorialKeyHighlighted(i,TutorialStage(chart.SecondsToBeat(Mathf.Max(0,SongTime))));
                Color c=i<2?new Color(.22f,.72f,.89f):new Color(.63f,.41f,.85f);
                c.a=held[i]?1f:guide?.72f:.32f;
                visual.SetColor("Game_KeyBG_"+i,c);
            }
            BeginNotes();
            DrawNotes(false);
            DrawBursts();
            EndNotes();
        }
        void RefreshEditorVisual()
        {
            visual.SetText("Editor_TrackLabel",chart.title);
            visual.SetText("B_Editor_Play_Label",playing?"PAUSE":"PLAY");
            visual.SetText("Editor_FooterText","BEAT "+chart.SecondsToBeat(Mathf.Max(0,SongTime)).ToString("0.00")+
                "      SNAP 1/"+(snapDivision*4)+"      ZOOM "+Mathf.RoundToInt(editorPxPerBeat)+"      NOTES "+chart.notes.Count);
            for(int i=0;i<editorTools.Length;i++) visual.SetColor("B_Tool_"+i,currentTool==editorTools[i]?
                new Color(.13f,.39f,.52f):new Color(.084f,.115f,.19f));
            for(int i=0;i<editorSnaps.Length;i++) visual.SetColor("B_Snap_"+i,snapDivision==editorSnaps[i]?
                new Color(.13f,.39f,.52f):new Color(.084f,.115f,.19f));
            if(lastVisualTab!=inspectorTab)
            {
                lastVisualTab=inspectorTab;
                string[] names={"Audio","Timing","Edit"};
                for(int i=0;i<3;i++)
                {
                    var obj=visual.Obj("Editor_Tab_"+names[i]);if(obj!=null)obj.SetActive((int)inspectorTab==i);
                }
            }
            for(int i=0;i<3;i++)visual.SetColor("B_Tab_"+i,(int)inspectorTab==i?
                new Color(.13f,.39f,.52f):new Color(.084f,.115f,.19f));
            if(inspectorTab==InspectorTab.Audio)
            {
                visual.SetText("Audio_Title",activeImported!=null?activeImported.title:(song!=null?song.name:"NO AUDIO"));
                visual.SetText("Audio_Time",SongTime.ToString("0.000")+"s");
                visual.SetText("B_Loop_Toggle_Label",loopEnabled?"LOOP ON":"LOOP OFF");
                visual.SetText("Audio_LoopSummary",loopEnabled?"BEATS "+loopStartBeat.ToString("0.##")+" - "+loopEndBeat.ToString("0.##"):"");
                DrawWaveform();
            }
            else if(inspectorTab==InspectorTab.Timing)
            {
                visual.SetText("Timing_BPMValue",chart.bpm.ToString("0.##"));
                visual.SetText("Timing_OffsetValue",chart.offsetMs.ToString("0"));
                visual.SetText("Timing_ZoomValue",Mathf.RoundToInt(editorPxPerBeat)+" px/beat");
            }
            else
            {
                visual.SetText("Edit_Summary","NOTES  "+chart.notes.Count+"\nSELECTED  "+selectedIds.Count);
                visual.SetText("B_Edit_Clear_Label",confirmClear?"CONFIRM CLEAR":"CLEAR CHART");
            }
            BeginNotes();
            DrawEditorGridSprites();
            DrawNotes(true);
            EndNotes();
        }

        static int TutorialStage(float beat)
        {
            if(beat<18) return 0;if(beat<34)return 1;if(beat<50)return 2;
            if(beat<68)return 3;if(beat<85)return 4;return 5;
        }
        static bool TutorialKeyHighlighted(int input,int stage)
        {
            if(stage==0)return input<=1;if(stage==1)return input>=2;
            if(stage==2||stage==3)return input==1||input==2;
            return stage==4;
        }
        static int PoolLane(int input) => input==0?0:input==3?2:1;
        bool HasPair(ChartNote note,bool hold,bool editing=false)
        {
            if(note.input!=1&&note.input!=2)return false;
            foreach(var n in (editing?editorRenderNotes:playNotes))
                if(n.note.input==(note.input==1?2:1)&&!n.finished&&
                    (hold||!n.headDone)&&Mathf.Abs(n.note.beat-note.beat)<.0001f&&
                    Mathf.Abs(n.note.durationBeats-note.durationBeats)<.0001f&&
                    (n.note.durationBeats>0)==hold) return true;
            return false;
        }
        Sprite NoteSprite(bool white,bool dual,bool hold)
        {
            DuovexTheme t=visual.theme;
            if(t==null)return null;
            return dual?t.dualTap:hold?(white?t.whiteHold:t.blackHold):(white?t.whiteTap:t.blackTap);
        }
        void BeginNotes(){noteCursor=0;}
        Image PooledImage(RectTransform parent,float x,float y,float w,float h,Sprite sprite,Color tint)
        {
            if(w<.1f||h<.1f)return null;
            Image img;
            if(noteCursor>=notePool.Count)
            {
                img=DuovexUIFactory.Img(parent,"DynamicNote_"+noteCursor,x,y,w,h,sprite,tint);
                notePool.Add(img);
            }
            else
            {
                img=notePool[noteCursor];
                if(img.transform.parent!=parent)img.transform.SetParent(parent,false);
                img.gameObject.SetActive(true);
                DuovexUIFactory.SetRect((RectTransform)img.transform,x,y,w,h);
                img.sprite=sprite;img.color=tint;
            }
            noteCursor++;
            return img;
        }
        void EndNotes()
        {
            for(int i=noteCursor;i<notePool.Count;i++) if(notePool[i]!=null)notePool[i].gameObject.SetActive(false);
        }
        void Note(RectTransform layer,float x,float y,float w,bool white,bool dual=false,bool selected=false)
        {
            float h=visual.theme!=null?visual.theme.noteHeight:17f;
            PooledImage(layer,x,y,w,h,NoteSprite(white,dual,false),Color.white);
            if(selected) PooledImage(layer,x,y+h+2,w,3,null,new Color(1f,.78f,.28f));
        }
        void HoldBody(RectTransform layer,float x,float head,float tail,float w,bool white,bool active,float top,float bottom)
        {
            float y=Mathf.Max(top,tail),b=Mathf.Min(bottom,head);
            if(b<=y)return;
            Color c=white?new Color(.40f,.83f,1f,active?.66f:.29f):new Color(.68f,.50f,1f,active?.66f:.29f);
            PooledImage(layer,x,y,w,b-y,NoteSprite(white,false,true),c);
        }
        void DrawNotes(bool editing)
        {
            float left=editing?EditorLaneX:LaneX,w=editing?EditorLaneW:LaneW,
                top=editing?EditorLaneTop:LaneTop,bottom=editing?EditorHitY:HitY;
            var layer=visual.Get<RectTransform>(editing?"Editor_NotesLayer":"Game_NotesLayer");
            if(layer==null||chart==null)return;
            float time=SongTime;
            float inset=visual.theme!=null?visual.theme.noteSideInset:6f;
            if(editing)
            {
                editorRenderNotes.Clear();
                foreach(var n in chart.notes) editorRenderNotes.Add(new PlayNote { note=n });
            }
            var notes=editing?editorRenderNotes:playNotes;
            foreach(var p in notes)
            {
                if(!editing && (p.finished||p.note.durationBeats<=0))continue;
                if(editing && p.note.durationBeats<=0)continue;
                float head=editing?YAtBeat(p.note.beat):bottom-(chart.BeatToSeconds(p.note.beat)-time)*ScrollPxPerSecond*noteSpeed;
                float tail=editing?YAtBeat(p.note.beat+p.note.durationBeats):bottom-(chart.BeatToSeconds(p.note.beat+p.note.durationBeats)-time)*ScrollPxPerSecond*noteSpeed;
                if(head<top-35||tail>bottom+30)continue;
                bool paired=(p.note.input==1||p.note.input==2)&&HasPair(p.note,true,editing);
                float x=left+PoolLane(p.note.input)*w+inset;
                float width=w-2*inset;
                if(paired){width*=.5f;if(p.note.input==2)x+=width;}
                HoldBody(layer,x,head,tail,width,IsWhite(p.note.input),editing||held[p.note.input]&&p.headDone,top,bottom);
            }
            foreach(var p in notes)
            {
                if(p.note.durationBeats>0 && (editing||!p.headDone) && !p.finished)
                {
                    float y=editing?YAtBeat(p.note.beat):bottom-(chart.BeatToSeconds(p.note.beat)-time)*ScrollPxPerSecond*noteSpeed;
                    if(y<top-17||y>bottom+17)continue;
                    bool paired=(p.note.input==1||p.note.input==2)&&HasPair(p.note,true,editing);
                    if(paired && p.note.input==2)continue;
                    Note(layer,left+PoolLane(p.note.input)*w+inset,y,w-2*inset,IsWhite(p.note.input),paired,editing&&selectedIds.Contains(p.note.id));
                }
            }
            foreach(var p in notes)
            {
                if(p.note.durationBeats>0||(!editing&&(p.finished||p.headDone)))continue;
                if(p.note.input==2&&HasPair(p.note,false,editing))continue;
                float y=editing?YAtBeat(p.note.beat):bottom-(chart.BeatToSeconds(p.note.beat)-time)*ScrollPxPerSecond*noteSpeed;
                if(y<top-17||y>bottom+17)continue;
                bool paired=p.note.input==1&&HasPair(p.note,false,editing);
                Note(layer,left+PoolLane(p.note.input)*w+inset,y,w-2*inset,IsWhite(p.note.input),paired,editing&&selectedIds.Contains(p.note.id));
            }
        }
        void DrawBursts()
        {
            var layer=visual.Get<RectTransform>("Game_NotesLayer");
            if(layer==null)return;
            foreach(var b in bursts)
            {
                float t=Mathf.Clamp01((Time.unscaledTime-b.birth)/.27f);
                float x=LaneX+PoolLane(b.input)*LaneW,w=LaneW;
                if(b.input==1)w*=.5f;
                if(b.input==2){x+=LaneW*.5f;w*=.5f;}
                Color c=IsWhite(b.input)?new Color(.30f,.89f,1f,1f-t):new Color(.72f,.46f,1f,1f-t);
                PooledImage(layer,x-16-t*12,HitY-32-t*26,w+32+t*24,48+t*30,visual.theme!=null?visual.theme.glow:null,c);
            }
        }
        void DrawEditorGridSprites()
        {
            var layer=visual.Get<RectTransform>("Editor_NotesLayer");if(layer==null)return;
            float last=editorBaseBeat+(EditorHitY-EditorLaneTop)/editorPxPerBeat;
            int stride=Mathf.Max(1,Mathf.CeilToInt(7f*snapDivision/editorPxPerBeat));
            int first=Mathf.Max(0,Mathf.CeilToInt(editorBaseBeat*snapDivision/stride)*stride);
            for(int s=first;s<=Mathf.CeilToInt(last*snapDivision);s+=stride)
            {
                float beat=s/(float)snapDivision,y=YAtBeat(beat);
                if(y<EditorLaneTop||y>EditorHitY)continue;
                bool main=s%(snapDivision*4)==0;
                PooledImage(layer,EditorLaneX,y,EditorLaneW*3,main?2f:1f,null,
                    main?new Color(.58f,.76f,1f,.40f):new Color(.38f,.48f,.70f,.16f));
            }
        }
        void DrawWaveform()
        {
            RectTransform layer=visual.Get<RectTransform>("WaveformLayer");if(layer==null)return;
            int count=waveReady&&waveform!=null&&song!=null?94:0;
            for(int i=0;i<count;i++)
            {
                Image img;
                if(i>=wavePool.Count) {img=DuovexUIFactory.Img(layer,"WaveBar_"+i,1070,316+i*2.3f,1,2,null,Color.cyan);wavePool.Add(img);}
                else {img=wavePool[i];img.gameObject.SetActive(true);}
                float beat=editorBaseBeat+(i/94f)*(EditorHitY-EditorLaneTop)/editorPxPerBeat;
                float seconds=chart.BeatToSeconds(beat);
                int index=Mathf.RoundToInt(seconds/Mathf.Max(.01f,song.length)*(waveform.Length-1));
                float amplitude=index>=0&&index<waveform.Length?waveform[index]:0;
                float size=Mathf.Clamp01(amplitude*2f)*144f;
                DuovexUIFactory.SetRect((RectTransform)img.transform,1073-size,315+i*2.4f,size*2f,2f);
                img.color=new Color(.34f,.82f,1f,.66f);
            }
            for(int i=count;i<wavePool.Count;i++)wavePool[i].gameObject.SetActive(false);
        }
    }
}
