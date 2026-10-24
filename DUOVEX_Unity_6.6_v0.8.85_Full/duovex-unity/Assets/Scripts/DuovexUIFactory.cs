using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace DualSync
{
    // Editable in the Inspector: replace each sprite, adjust colors and save the asset.
    [CreateAssetMenu(menuName = "Duovex/Visual Theme", fileName = "DuovexTheme")]
    public sealed class DuovexTheme : ScriptableObject
    {
        public Sprite background, panel, button, lane, hitLine;
        public Sprite menuOrbital, duovexMark;
        public Sprite whiteTap, blackTap, dualTap, whiteHold, blackHold, glow;
        public Color cyan = new Color(.40f, .88f, 1f);
        public Color violet = new Color(.70f, .50f, 1f);
        public Color foreground = new Color(.92f, .96f, 1f);
        public Color muted = new Color(.55f, .64f, .78f);
        public Font font;
        [Range(8f,40f)] public float noteHeight = DuovexUIFactory.GameNoteHeight;
        [Range(0f,18f)] public float noteSideInset = DuovexUIFactory.GameNoteInset;
    }

    // All static UI is ordinary Canvas/Image/Text/Button geometry, not an OnGUI surface.
    // Editor menu Tools > Duovex > Rebuild Editable Scene persists this hierarchy to a scene.
    public static class DuovexUIFactory
    {
        // v0.7.2 master-layout coordinates (1280x720). Keep the renderer's
        // saved-RectTransform behavior so artists can adjust their scene safely.
        public const float GameGearX = 496f;
        public const float GameGearTop = 78f;
        public const float GameLaneWidth = 96f;
        public const float GameHitCenterY = 586f;
        public const float GameKeyTop = 618f;
        public const float GameNoteHeight = 16f;
        public const float GameNoteInset = 6f;
        public static readonly Color SongSelected = new Color(.075f, .31f, .41f, .97f);
        public static readonly Color SongUnselected = new Color(.036f, .078f, .15f, .96f);
        static DuovexTheme theme;
        static Font font;
        static Color cyan, violet;
        static readonly Color dark = new Color(.046f, .062f, .11f, .96f);
        static readonly Color panel = Color.white;
        static readonly Color panelAlt = new Color(.43f, .61f, .78f, .90f);

        public static DuovexCanvasView Build(DuovexTheme visualTheme = null)
        {
            theme = visualTheme != null ? visualTheme : Resources.Load<DuovexTheme>("Visual/DuovexTheme");
            // The old Main scene still launches: auto-create a runtime-only fallback theme.
            // To save/edit the hierarchy, run Tools > Duovex > Generate Editable UI Scene.
            if(theme==null)
            {
                theme=ScriptableObject.CreateInstance<DuovexTheme>();
                theme.background=Resources.Load<Sprite>("Visual/Background");
                theme.menuOrbital=Resources.Load<Sprite>("Visual/MenuOrbital");
                theme.duovexMark=Resources.Load<Sprite>("Visual/DuovexMark");
                theme.panel=Resources.Load<Sprite>("Visual/Panel");
                theme.button=Resources.Load<Sprite>("Visual/Button");
                theme.lane=Resources.Load<Sprite>("Visual/Lane");
                theme.hitLine=Resources.Load<Sprite>("Visual/HitLine");
                theme.whiteTap=Resources.Load<Sprite>("Visual/WhiteTap");
                theme.blackTap=Resources.Load<Sprite>("Visual/BlackTap");
                theme.dualTap=Resources.Load<Sprite>("Visual/DualTap");
                theme.whiteHold=Resources.Load<Sprite>("Visual/WhiteHold");
                theme.blackHold=Resources.Load<Sprite>("Visual/BlackHold");
                theme.glow=Resources.Load<Sprite>("Visual/Glow");
            }
            font = theme != null ? theme.font : null;
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch (Exception) { }
                if(font==null) font=Font.CreateDynamicFontFromOSFont("Arial",16);
            }
            cyan = theme != null ? theme.cyan : new Color(.38f, .90f, 1f);
            violet = theme != null ? theme.violet : new Color(.7f, .5f, 1f);

            var go = new GameObject("DUOVEX_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // Actual full-screen backdrop fills non-16:9 windows too. The original
            // 1280x720 DesignRoot remains stable for the editable UI coordinates.
            var fullBackdrop=new GameObject("FullscreenBackdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            fullBackdrop.transform.SetParent(go.transform,false);
            var fullRect=(RectTransform)fullBackdrop.transform;
            fullRect.anchorMin=Vector2.zero;fullRect.anchorMax=Vector2.one;
            fullRect.offsetMin=Vector2.zero;fullRect.offsetMax=Vector2.zero;
            fullBackdrop.sprite=theme!=null?theme.background:null;
            fullBackdrop.color=Color.white; fullBackdrop.raycastTarget=false;
            var design = new GameObject("DesignRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            design.SetParent(go.transform, false);
            design.anchorMin = design.anchorMax = new Vector2(.5f, .5f);
            design.pivot = new Vector2(.5f, .5f);
            design.sizeDelta = new Vector2(1280, 720);
            design.anchoredPosition = Vector2.zero;
            design.localScale = Vector3.one;
            Img(design, "Background", 0, 0, 1280, 720, StyleSprite("BgSelect"), Color.white);
            Img(design, "TopBar", 0, 0, 1280, 53, null, new Color(.035f, .034f, .06f, .94f));
            Img(design, "TopAccent", 0, 51, 1280, 2, null, new Color(.63f, .36f, .95f, .85f));
            Label(design, "Brand", "DUOVEX", 57, 12, 240, 30, 22, Color.white, TextAnchor.MiddleLeft);
            Img(design, "BrandMark", 20, 11, 38, 38, StyleSprite87("DuovexNeonMark87") != null ? StyleSprite87("DuovexNeonMark87") : (theme != null ? theme.duovexMark : null), Color.white);
            Label(design, "HeaderEdition", "// RHYTHM", 1085, 14, 160, 24, 13, new Color(.42f,.53f,.69f),TextAnchor.MiddleRight);

            // New v0.8.2 layouts are built from editable Unity UI components.
            // Only cosmetic elements change: all stable gameplay/button IDs remain.
            BuildMain087(design);
            BuildSelect083(design);

            var options = Group(design, "Page_Options");
            StyleImage(options,"Options_Background","BgSelect",0,0,1280,720);
            Label(options, "Options_Header", "OPTIONS", 100, 95, 460, 49, 33, cyan);
            StyleImage(options,"Options_Panel","InfoDark",100,190,1080,406);
            Label(options, "Options_Music", "MUSIC VOLUME", 153, 225, 330, 28, 22, Color.white);
            var musicSlider = CreateSlider(options, "Slider_Music", 153, 268, 570, 23);
            var musicFill=musicSlider.transform.Find("Slider_Music_Fill").GetComponent<Image>();
            musicFill.color=new Color(.58f,.57f,1f);
            musicSlider.value = .80f;
            Label(options, "Options_MusicValue", "80%", 756, 260, 110, 34, 20, new Color(.67f,.68f,1f));
            Label(options, "Options_SFX", "HIT SOUND VOLUME", 153, 316, 330, 28, 22, Color.white);
            var sound = CreateSlider(options, "Slider_SFX", 153, 358, 570, 23);
            sound.value = .32f;
            Label(options, "Options_Volume", "32%", 756, 350, 110, 35, 20, cyan);
            CompactButton(options, "B_White_Preview", "WHITE PREVIEW", 154, 406, 235, 38);
            CompactButton(options, "B_Black_Preview", "BLACK PREVIEW", 402, 406, 235, 38);
            Label(options, "Options_Speed", "SCROLL SPEED", 154, 478, 275, 38, 19, Color.white);
            CompactButton(options, "B_Speed_Minus", "-", 483, 480, 50, 41);
            Label(options, "Options_SpeedValue", "1.0x", 548, 480, 100, 41, 23, cyan, TextAnchor.MiddleCenter);
            CompactButton(options, "B_Speed_Plus", "+", 670, 480, 50, 41);
            Btn(options, "B_Options_Back", "BACK", 935, 529, 209, 48);

            var result = Group(design, "Page_Result");
            Image83(result,"Result_Background","BgSelect83",0,0,1280,720);
            Img(result,"Result_Overlay",0,53,1280,667,null,new Color(.012f,.014f,.030f,.79f));
            StyleImage(result,"Result_Panel","InfoDark",191,101,898,529);
            Img(result,"Result_TopBar",221,132,838,4,null,new Color(.66f,.36f,.99f));
            Label(result,"Result_Heading","CLEAR",226,145,680,100,73,new Color(.65f,.90f,1f));
            Label(result,"Result_Grade","S",933,153,102,95,74,new Color(.76f,.53f,1f),TextAnchor.MiddleCenter);
            Label(result,"Result_ScoreCaption","TOTAL SCORE / 1,000,000",231,267,468,26,15,new Color(.68f,.72f,.84f));
            Label(result,"Result_Score","0000000",227,297,610,86,65,Color.white);
            Label(result,"Result_AccLabel","ACCURACY",232,392,250,26,15,new Color(.66f,.75f,.88f));
            Label(result,"Result_Accuracy","00.00%",230,423,386,66,46,new Color(.47f,.91f,1f));
            Label(result,"Result_Summary","MAX COMBO  0\nMISS  0",737,382,284,99,20,Color.white);
            Img(result,"Result_StatisticsDivider",230,503,822,2,null,new Color(.45f,.48f,.64f,.75f));
            Label(result,"Result_Breakdown","PERFECT 0  GREAT 0  GOOD 0  BAD 0  MISS 0",232,514,822,46,15,new Color(.79f,.80f,.91f));
            Btn(result,"B_Result_Retry","RETRY",242,567,244,47,true);
            Btn(result,"B_Result_Select","TRACKS",516,567,244,47);
            Btn(result,"B_Result_Main","MENU",789,567,244,47);

            var browser = Group(design, "Page_Browser");
            Label(browser, "Browser_Header", "IMPORT AUDIO", 72, 88, 705, 53, 32, cyan);
            Img(browser, "Browser_Panel", 65, 157, 1150, 475, theme != null ? theme.panel : null, panel);
            Input(browser, "Input_BrowserPath", 91, 204, 826, 42);
            Btn(browser, "B_Browser_Go", "GO", 933, 204, 108, 42, true);
            Btn(browser, "B_Browser_Cancel", "BACK", 1052, 204, 119, 42);
            Btn(browser, "B_Browser_Up", "UP", 93, 258, 116, 32);
            Btn(browser, "B_Browser_Music", "MUSIC", 218, 258, 126, 32);
            Btn(browser, "B_Browser_Downloads", "DOWNLOADS", 351, 258, 163, 32);
            Btn(browser, "B_Browser_Home", "HOME", 522, 258, 127, 32);
            for (int i=0;i<7;i++) Btn(browser,"B_Browser_Row_"+i,"",93,307+i*40,866,35);
            Btn(browser, "B_Browser_Prev", "PREV", 1007, 344, 173, 49);
            Btn(browser, "B_Browser_Next", "NEXT", 1007, 409, 173, 49);
            Btn(browser, "B_Browser_Import", "IMPORT PATH", 1007, 478, 173, 52, true);
            Label(browser, "Browser_Error", "", 94, 593, 955, 31, 13, new Color(1f,.64f,.65f));

            var game = Group(design, "Page_Game");
            // Lower contrast behind the gameplay gear; the notes remain the focal point.
            Img(game,"Game_AmbientScrim",0,53,1280,615,null,new Color(.024f,.030f,.050f,.12f));
            Img(game,"Game_GearHalo",433,59,414,590,theme != null ? theme.glow : null,
                new Color(.35f,.58f,.92f,.18f));
            Img(game,"Game_GearPlate",482,69,316,585,theme != null ? theme.panel : null,
                new Color(.83f,.92f,1f,.79f));
            BuildPlayfield(game, "Game", GameGearX, GameGearTop, GameLaneWidth, GameHitCenterY, true);
            Label(game,"Game_Header","CURRENT TRACK",62,105,334,26,13,new Color(.54f,.65f,.78f));
            Label(game,"Game_Track","",62,135,345,68,23,Color.white);
            Label(game,"Game_AccuracyLabel","ACCURACY",62,237,330,29,13,new Color(.52f,.64f,.80f));
            Label(game,"Game_Accuracy","",62,264,350,77,48,cyan);
            Label(game,"Game_ComboLabel","COMBO",884,233,304,25,14,new Color(.52f,.64f,.80f));
            Label(game,"Game_Combo","",882,258,326,80,56,Color.white);
            Label(game,"Game_Score","",882,340,325,40,19,Color.white);
            Label(game,"Game_Speed","",882,405,315,30,15,cyan);
            Label(game,"Game_Fast","",GameGearX,399,GameLaneWidth*3,25,13,new Color(.65f,.86f,1f),TextAnchor.MiddleCenter);
            Label(game,"Game_Judge","",GameGearX,426,GameLaneWidth*3,47,32,Color.white,TextAnchor.MiddleCenter);
            Label(game,"Game_Slow","",GameGearX,468,GameLaneWidth*3,25,13,new Color(1f,.70f,.76f),TextAnchor.MiddleCenter);
            Label(game,"Game_Guide","",64,493,326,84,18,Color.white);
            Img(game,"Game_ProgressBack",51,657,1178,3,null,new Color(.19f,.25f,.4f));
            Img(game,"Game_Progress",51,657,0,3,theme != null ? theme.hitLine : null,cyan);
            Label(game,"Game_Time","",975,658,285,30,13,theme != null ? theme.muted : Color.gray);
            Img(game,"Game_PauseDim",0,53,1280,615,null,new Color(.005f,.011f,.028f,.77f));
            StyleImage(game,"Game_PausePanel","InfoDark",422,203,436,311);
            Img(game,"Game_PauseTopEdge",422,203,436,3,null,new Color(.36f,.92f,1f));
            Label(game,"Game_Paused","PAUSED",458,228,364,59,39,cyan,TextAnchor.MiddleCenter);
            Label(game,"Game_PauseHint","ESC / SPACE TO RESUME",458,284,364,27,14,
                new Color(.72f,.82f,.91f),TextAnchor.MiddleCenter);
            FeatureButton(game,"B_Pause_Resume","RESUME","01",470,324,340,54,true);
            CompactButton(game,"B_Pause_Retry","RETRY",470,392,161,48);
            CompactButton(game,"B_Pause_Tracks","TRACK SELECT",648,392,162,48);

            var editor = Group(design,"Page_Editor");
            Label(editor,"Editor_Title","CHART STUDIO",28,64,550,48,30,cyan);
            Btn(editor,"B_Editor_Play","PLAY",710,57,101,43,true);
            Btn(editor,"B_Editor_Test","TEST",822,57,99,43);
            Btn(editor,"B_Editor_Save","SAVE",933,57,100,43);
            Btn(editor,"B_Editor_Rewind","REWIND",1043,57,103,43);
            Btn(editor,"B_Editor_Exit","EXIT",1158,57,96,43);
            Img(editor,"Editor_ToolPanel",22,144,232,486,theme != null ? theme.panel : null,panel);
            Img(editor,"Editor_InspectorPanel",895,144,358,486,theme != null ? theme.panel : null,panel);
            Label(editor,"Editor_ToolsLabel","TOOLS",38,157,197,29,18,cyan);
            string[] tools={"SELECT","WHITE","BLACK","DUAL","W HOLD","B HOLD","D HOLD"};
            for(int i=0;i<tools.Length;i++) Btn(editor,"B_Tool_"+i,(i+1)+"  "+tools[i],39,191+i*43,201,35);
            Label(editor,"Editor_SnapLabel","SNAP",39,515,198,25,15,cyan);
            for(int i=0;i<6;i++) Btn(editor,"B_Snap_"+i,"1/"+(4*(1<<i)),39+i%3*68,540+i/3*38,63,32);
            BuildPlayfield(editor,"Editor",319,169,178,608,false);
            Label(editor,"Editor_TrackLabel","",317,130,543,32,13,Color.white,TextAnchor.MiddleCenter);
            Img(editor,"Editor_Footer",0,649,1280,71,null,new Color(.022f,.033f,.06f,.96f));
            Label(editor,"Editor_FooterText","",28,657,1216,40,14,theme != null ? theme.muted : Color.gray);
            for(int i=0;i<3;i++) Btn(editor,"B_Tab_"+i, new[]{"AUDIO","TIMING","EDIT"}[i],911+i*111,157,106,35);

            var audio=Group(editor,"Editor_Tab_Audio");
            Label(audio,"Audio_Title","",911,215,323,32,18,Color.white);
            Label(audio,"Audio_Time","",911,252,320,25,15,cyan);
            Label(audio,"Audio_WaveTitle","WAVEFORM",912,284,321,24,15,theme != null ? theme.muted : Color.gray);
            Img(audio,"Audio_WaveBackground",911,310,323,234,null,new Color(.03f,.05f,.09f));
            Group(audio,"WaveformLayer");
            Btn(audio,"B_Loop_In","IN",911,559,76,33);
            Btn(audio,"B_Loop_Out","OUT",998,559,76,33);
            Btn(audio,"B_Loop_Toggle","LOOP OFF",1085,559,149,33);
            Label(audio,"Audio_LoopSummary","",911,600,326,27,14,cyan);

            var timing=Group(editor,"Editor_Tab_Timing");
            Label(timing,"Timing_BPMLabel","BPM",915,222,260,30,15,cyan);
            Label(timing,"Timing_BPMValue","120",915,256,154,54,32,Color.white);
            Btn(timing,"B_BPM_Down","-",1113,254,50,37);
            Btn(timing,"B_BPM_Up","+",1174,254,50,37);
            Label(timing,"Timing_OffsetLabel","OFFSET (ms)",915,337,290,31,15,cyan);
            Label(timing,"Timing_OffsetValue","0",915,372,170,44,29,Color.white);
            Btn(timing,"B_Offset_Down","-",1113,371,50,37);
            Btn(timing,"B_Offset_Up","+",1174,371,50,37);
            Label(timing,"Timing_ZoomLabel","ZOOM",915,452,290,32,15,cyan);
            Label(timing,"Timing_ZoomValue","58 px/beat",915,489,193,37,17,Color.white);
            Btn(timing,"B_Zoom_Down","-",1113,487,50,37);
            Btn(timing,"B_Zoom_Up","+",1174,487,50,37);

            var edit=Group(editor,"Editor_Tab_Edit");
            Label(edit,"Edit_Summary","",914,220,304,58,18,Color.white);
            Btn(edit,"B_Edit_Undo","UNDO",914,301,148,39);
            Btn(edit,"B_Edit_Redo","REDO",1082,301,148,39);
            Btn(edit,"B_Edit_Copy","COPY",914,352,148,39);
            Btn(edit,"B_Edit_Paste","PASTE",1082,352,148,39);
            Btn(edit,"B_Edit_Delete","DELETE",914,403,148,39);
            Btn(edit,"B_Edit_Load","LOAD",1082,403,148,39);
            Btn(edit,"B_Edit_Clear","CLEAR CHART",914,484,316,39);
            Btn(edit,"B_Edit_Demo","LOAD DEMO",914,544,316,39);

            var view=go.AddComponent<DuovexCanvasView>();
            view.theme=theme;
            EnsureEvents();
            return view;
        }

        static void BuildPlayfield(RectTransform page, string prefix, float x, float y, float laneW, float hitY, bool gameplay)
        {
            // Three physical lanes. The center remains one shared lane, not two narrower lanes.
            var laneTint = new Color(1f,1f,1f,.97f);
            for(int i=0;i<3;i++)
            {
                var img=Img(page,prefix+"_Lane_"+i,x+i*laneW,y,laneW,hitY-y,
                    theme!=null?theme.lane:null,laneTint);
                // The lane graphic should never catch mouse events in the chart editor.
                img.raycastTarget=false;
            }
            // A proper masked viewport keeps notes and holds inside the lane gear.
            // Its RectTransform is synchronized to the saved lane RectTransforms by the renderer.
            var viewport=Group(page,prefix+"_NotesLayer");
            SetRect(viewport,x,y,laneW*3,hitY-y+14f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(page,prefix+"_HitGlow",x-14,hitY-22,laneW*3+28,44,
                theme!=null?theme.glow:null,new Color(.40f,.86f,1f,gameplay ? .72f : .35f));
            // All visible line layers have the SAME center Y as the note heads.
            // The solid shapes avoid the low-contrast/blurred PNG issue.
            if(gameplay)
            {
                Img(page,"Game_HitUnderlay",x-2,hitY-6,laneW*3+4,12,null,
                    new Color(.008f,.022f,.055f,1f));
                Img(page,prefix+"_HitLine",x,hitY-4,laneW*3,8,null,
                    new Color(.19f,.87f,1f,1f));
                Img(page,"Game_HitCore",x,hitY-1,laneW*3,2,null,Color.white);
                Img(page,"Game_HitCapLeft",x-3,hitY-11,3,22,null,new Color(.43f,.94f,1f));
                Img(page,"Game_HitCapRight",x+laneW*3,hitY-11,3,22,null,new Color(.43f,.94f,1f));
            }
            else
                Img(page,prefix+"_HitLine",x,hitY-4,laneW*3,8,
                    theme!=null?theme.hitLine:null,Color.white);
            if(gameplay)
            {
                string[] names={"D","F","J","K"};
                for(int i=0;i<4;i++)
                {
                    float px=i==0?x:i==1?x+laneW:i==2?x+laneW*1.5f:x+laneW*2f;
                    float w=i==0||i==3?laneW:laneW*.5f;
                    float keyTop=hitY+(GameKeyTop-GameHitCenterY);
                    Img(page,"Game_KeyBG_"+i,px+2,keyTop,w-4,29,null,
                        i<2?new Color(.12f,.42f,.55f,.80f):new Color(.33f,.20f,.52f,.80f));
                    Img(page,"Game_KeyCapBar_"+i,px+2,keyTop,w-4,2,null,
                        i<2?new Color(.37f,.86f,1f):new Color(.79f,.57f,1f));
                    Label(page,"Game_Key_"+i,names[i],px+2,keyTop+1,w-4,28,17,Color.white,TextAnchor.MiddleCenter);
                }
            }
        }

        public static RectTransform Group(Transform parent, string id)
        {
            var t=new GameObject(id,typeof(RectTransform)).GetComponent<RectTransform>();
            t.SetParent(parent,false);
            t.anchorMin=t.anchorMax=new Vector2(0,1); t.pivot=new Vector2(0,1);
            t.anchoredPosition=Vector2.zero; t.sizeDelta=new Vector2(1280,720); t.localScale=Vector3.one;
            return t;
        }
        public static RectTransform SetRect(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); r.localScale=Vector3.one;
            return r;
        }
        public static Image Img(Transform parent,string id,float x,float y,float w,float h,Sprite sprite,Color color)
        {
            var i=new GameObject(id,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            i.transform.SetParent(parent,false);
            SetRect((RectTransform)i.transform,x,y,w,h);
            i.sprite=sprite; i.color=color; i.raycastTarget=false;
            return i;
        }
        static Text Label(Transform parent,string id,string value,float x,float y,float w,float h,int size,Color color,
            TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=new GameObject(id,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent,false);
            SetRect((RectTransform)t.transform,x,y,w,h);
            t.font=font; t.text=value; t.fontSize=size; t.fontStyle=size>=18?FontStyle.Bold:FontStyle.Normal; t.color=color;
            t.alignment=align; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; t.raycastTarget=false;
            return t;
        }
        static Button Btn(Transform parent,string id,string label,float x,float y,float w,float h,bool primary=false)
        {
            Image img=Img(parent,id,x,y,w,h,StyleSprite(primary?"BtnWhite":"BtnDark"),Color.white);
            img.raycastTarget=true;
            var b=img.gameObject.AddComponent<Button>(); b.targetGraphic=img;
            img.gameObject.AddComponent<DuovexButtonMotion>();
            b.transition=Selectable.Transition.None; // animated by DuovexButtonMotion
            Label(img.transform,id+"_Label",label,0,0,w,h,17,primary?new Color(.045f,.035f,.075f):Color.white,TextAnchor.MiddleCenter);
            return b;
        }
        // Angled action buttons are transparent PNG sprites + normal Unity Buttons.
        // The same IDs as the original UI are preserved for the gameplay controller.
        static Button FeatureButton(Transform parent,string id,string label,string number,
            float x,float y,float w,float h,bool primary=false)
        {
            Button button=Btn(parent,id,label,x,y,w,h,primary);
            var txt=button.transform.Find(id+"_Label").GetComponent<Text>();
            txt.fontSize=20;txt.fontStyle=FontStyle.Bold;
            SetRect((RectTransform)txt.transform,65,0,w-127,h);
            txt.alignment=TextAnchor.MiddleLeft;
            Label(button.transform,id+"_Number",number,30,0,36,h,12,
                primary?new Color(.36f,.19f,.53f):new Color(.68f,.55f,.89f),TextAnchor.MiddleCenter);
            Label(button.transform,id+"_Arrow",">",w-65,0,40,h,24,
                primary?new Color(.33f,.21f,.52f):Color.white,TextAnchor.MiddleCenter);
            return button;
        }
        static Button CompactButton(Transform parent,string id,string label,float x,float y,float w,float h)
        {
            Button button=Btn(parent,id,label,x,y,w,h);
            var txt=button.transform.Find(id+"_Label").GetComponent<Text>();
            txt.fontSize=15; txt.fontStyle=FontStyle.Bold;
            return button;
        }

        static readonly Dictionary<string,Sprite> styleSpriteCache=new Dictionary<string,Sprite>();
        public static Sprite StyleSprite(string file)
        {
            Sprite result;
            if(styleSpriteCache.TryGetValue(file,out result))return result;
            string path="Visual082/"+file;
            result=Resources.Load<Sprite>(path);
            // Runtime fallback for projects opened before the v0.8.2 importer ran.
            if(result==null)
            {
                Texture2D texture=Resources.Load<Texture2D>(path);
                if(texture!=null)
                    result=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),
                        new Vector2(.5f,.5f),100f);
            }
            styleSpriteCache[file]=result;
            return result;
        }
        static Image StyleImage(Transform parent,string id,string file,float x,float y,float w,float h)
        {
            return Img(parent,id,x,y,w,h,StyleSprite(file),Color.white);
        }
        static void StyleHeader(Transform page,string title,string subtitle)
        {
            Img(page,"Neo_HeaderBackdrop",0,0,1280,62,null,new Color(.026f,.025f,.044f,.96f));
            Img(page,"Neo_HeaderRule",0,59,1280,3,null,new Color(.59f,.32f,.91f));
            Label(page,"Neo_HeaderTitle",title,65,12,430,42,26,Color.white);
            Label(page,"Neo_HeaderSubtitle",subtitle,940,15,265,28,12,
                new Color(.75f,.67f,.85f),TextAnchor.MiddleRight);
        }
        // v0.8.3 - original monochrome X-composition and asymmetric menu concept.
        // Every functional control is a separate editable uGUI Button/Image/Text.
        // Decoration is always created BEFORE foreground labels and buttons.
        public static Sprite StyleSprite83(string name)
        {
            return Resources.Load<Sprite>("Visual083/"+name);
        }
        static Image Image83(Transform parent,string id,string sprite,
            float x,float y,float w,float h,Color? tint=null)
        {
            var art=StyleSprite83(sprite);
            if(art==null) art=StyleSprite(sprite);
            var img=Img(parent,id,x,y,w,h,art,tint??Color.white);
            img.raycastTarget=false;
            return img;
        }
        static Button Action83(Transform parent,string id,string name,string index,
            float x,float y,float w,float h,bool primary)
        {
            // Preserve the existing event ID, but replace its old UI art.
            Button button=Btn(parent,id,"",x,y,w,h,primary);
            var image=button.GetComponent<Image>();
            image.sprite=StyleSprite83(primary?"ActionPrimary83":"ActionSecondary83");
            image.color=Color.white;
            Text original=button.transform.Find(id+"_Label").GetComponent<Text>();
            original.text=name;
            original.fontSize=21;
            original.fontStyle=FontStyle.Bold;
            original.color=primary?new Color(.070f,.060f,.098f):new Color(.96f,.96f,1f);
            original.alignment=TextAnchor.MiddleLeft;
            original.horizontalOverflow=HorizontalWrapMode.Wrap;
            SetRect((RectTransform)original.transform,93,4,w-174,h-8);
            Label(button.transform,id+"_Index",index,53,3,35,h-6,13,
                primary?new Color(.38f,.22f,.52f):new Color(.69f,.49f,.90f),TextAnchor.MiddleCenter);
            Label(button.transform,id+"_Chevron",">",w-79,3,40,h-6,25,
                primary?new Color(.28f,.14f,.44f):Color.white,TextAnchor.MiddleCenter);
            return button;
        }
        static Button SmallAction83(Transform parent,string id,string label,float x,float y,float w,float h)
        {
            Button button=Btn(parent,id,"",x,y,w,h);
            button.GetComponent<Image>().sprite=StyleSprite83("ActionSmall83");
            Text tx=button.transform.Find(id+"_Label").GetComponent<Text>();
            tx.text=label;tx.fontSize=15;tx.fontStyle=FontStyle.Bold;
            tx.color=Color.white;
            SetRect((RectTransform)tx.transform,14,0,w-28,h);
            return button;
        }
        // v0.8.6: faithful interpretation of the supplied hand-drawn D-uovex
        // signature. No letters are prepainted into the background; art, buttons
        // and text are separate editable Image / Text / Button objects.
        public static Sprite StyleSprite86(string name)
        {
            return Resources.Load<Sprite>("Visual086/"+name);
        }
        static Image Image86(Transform parent,string id,string name,
            float x,float y,float w,float h,Color? tint=null)
        {
            return Img(parent,id,x,y,w,h,StyleSprite86(name),tint??Color.white);
        }
        static Button MenuAction86(Transform parent,string id,string title,string index,
            float x,float y,float w,float h,bool primary)
        {
            Button b=Btn(parent,id,"",x,y,w,h,primary);
            Image graphic=b.GetComponent<Image>();
            Sprite idle=StyleSprite86(primary?"MenuPrimary86":"MenuSecondary86");
            Sprite hover=StyleSprite86("MenuHover86");
            if(idle!=null)graphic.sprite=idle;
            var skin=b.gameObject.AddComponent<DuovexMenuButtonSkin86>();
            skin.idleSprite=idle; skin.hoverSprite=hover; skin.isPrimary=primary;
            Text label=b.transform.Find(id+"_Label").GetComponent<Text>();
            label.text=title;
            label.fontSize=24;
            label.fontStyle=FontStyle.Bold;
            label.alignment=TextAnchor.MiddleLeft;
            label.color=primary?new Color(.08f,.06f,.14f):new Color(.96f,.95f,1f);
            SetRect((RectTransform)label.transform,88,4,w-164,h-8);
            skin.title=label;
            Text num=Label(b.transform,id+"_Index",index,30,3,48,h-6,15,
                primary?new Color(.40f,.23f,.64f):new Color(.73f,.51f,.97f),TextAnchor.MiddleCenter);
            skin.index=num;
            Text arrow=Label(b.transform,id+"_Arrow",">",w-71,4,33,h-8,25,
                primary?new Color(.38f,.21f,.57f):new Color(.88f,.75f,1f),TextAnchor.MiddleCenter);
            skin.arrow=arrow;
            skin.Configure(idle,hover,primary,label,num,arrow);
            return b;
        }
        // v0.8.7: uses the *user-approved uploaded neon artwork* as a real
        // transparent Unity sprite. Separate Image objects remain editable.
        public static Sprite StyleSprite87(string name)
        {
            return Resources.Load<Sprite>("Visual087/"+name);
        }
        static Image Image87(Transform parent,string id,string name,
            float x,float y,float w,float h,Color? tint=null)
        {
            return Img(parent,id,x,y,w,h,StyleSprite87(name),tint??Color.white);
        }
        static void BuildMain087(RectTransform design)
        {
            var main=Group(design,"Page_Main");
            // Fully opaque, quiet backdrop. The actual logo is in its own
            // transparent PNG Image so Unity does not crop the edges.
            Image86(main,"Main_Background","BgMain86",0,0,1280,720);

            // Small logo derivative. Main_DV is an independently replaceable
            // Sprite in Inspector; keep ID for backwards-compatible animations.
            Image87(main,"Main_Orbital","DuovexNeonMark87",98,105,89,89,
                new Color(.65f,.72f,1f,.23f));
            Image87(main,"Main_DV","DuovexNeonMark87",104,111,77,77);
            Label(main,"Main_LogoSide","DUOVEX  /  RHYTHM SYSTEM",195,127,441,27,13,
                new Color(.70f,.72f,.85f));
            Label(main,"Main_IdentityHint","",0,0,0,0,12,Color.clear);

            // Direct application of the screenshot artwork supplied by the user.
            // Aspect ratio 1448:658 -> UI 688x313, with no stretching.
            var hero=Image87(main,"Main_Title","DuovexNeonLogo87",44,208,688,313);
            hero.preserveAspect=true;
            hero.raycastTarget=false;
            Img(main,"Main_TitleUnderline",103,533,503,2,null,
                new Color(.63f,.41f,.91f,.45f));
            Label(main,"Main_Subtitle","TWO HANDS. ONE SHARED LANE.",102,543,505,33,15,
                new Color(.75f,.76f,.87f));

            // Stable button IDs -- existing gameplay and navigation logic unchanged.
            MenuAction86(main,"B_Main_Play","START","01",760,215,430,75,true);
            MenuAction86(main,"B_Main_Edit","CHART STUDIO","02",760,333,430,75,false);
            MenuAction86(main,"B_Main_Options","SETTINGS","03",760,451,430,75,false);
            Label(main,"Main_MenuFooter","MUSIC  /  CHARTS  /  SETTINGS",780,623,425,47,13,
                new Color(.58f,.56f,.70f));
            Label(main,"Main_Footer","DUOVEX  /  NEON SIGNATURE",98,664,482,29,12,
                new Color(.56f,.52f,.67f));
            Label(main,"Main_Build","v0.8.8  /  PLAY POLISH",975,664,247,29,12,
                new Color(.56f,.52f,.67f),TextAnchor.MiddleRight);
            // Keep animation slots but turn off redundant flying marks.
            Img(main,"Main_Glyph",0,0,0,0,null,Color.clear);
            Img(main,"Main_Halo",0,0,0,0,null,Color.clear);
            main.gameObject.AddComponent<CanvasGroup>();
            main.gameObject.AddComponent<DuovexPageReveal>();
        }
        static void BuildSelect083(RectTransform design)
        {
            var select=Group(design,"Page_Select");
            Image83(select,"Select_Background","BgSelect83",0,0,1280,720);
            // Independent header, framed within the 1280x720 safe area.
            Img(select,"Select_HeaderBackdrop",0,0,1280,75,null,new Color(.015f,.020f,.041f,.95f));
            Image83(select,"Select_HeaderRule","Rule83",44,74,1183,2);
            Label(select,"Select_Header","SELECT TRACK",56,19,558,48,32,Color.white);
            Label(select,"Select_Serial","DUOVEX / MUSIC LIBRARY",891,21,330,40,14,
                new Color(.69f,.61f,.85f),TextAnchor.MiddleRight);
            // The reference song-select sketch: LEFT featured track, RIGHT vertical list.
            // Left light card is never overlaid with pale text.
            Image83(select,"Select_InfoPanel","InfoPanel83",54,104,555,512);
            Label(select,"Select_CoverLabel","NOW SELECTED",96,128,448,34,13,
                new Color(.44f,.30f,.63f));
            Image83(select,"Select_Cover","Cover3",94,182,210,210);
            Label(select,"Select_Title","LIGHT IT UP",330,187,239,127,25,
                new Color(.045f,.046f,.09f));
            Text selected=select.Find("Select_Title").GetComponent<Text>();
            selected.resizeTextForBestFit=true;
            selected.resizeTextMinSize=16; selected.resizeTextMaxSize=25;
            selected.horizontalOverflow=HorizontalWrapMode.Wrap;
            selected.verticalOverflow=VerticalWrapMode.Truncate;
            Label(select,"Select_BossLock","BOSS  //  LOCKED",330,377,247,31,16,
                new Color(.42f,.23f,.62f));
            Label(select,"Select_InfoDivider","TRACK INFORMATION",330,320,247,33,11,
                new Color(.46f,.44f,.60f));
            Image83(select,"Select_AccentRule","Rule83",330,356,201,2);
            Label(select,"Select_Meta","BPM 128",94,423,459,42,20,
                new Color(.11f,.10f,.17f));
            Label(select,"Select_Difficulty","SELECT DIFFICULTY",94,466,440,28,17,
                new Color(.34f,.15f,.51f));
            // One row per song; only EASY/NORMAL/HARD are selectable.
            // Archived BOSS maps remain on disk, with a visible lock notice.
            for (int difficulty=0;difficulty<3;difficulty++)
            {
                Button b=Btn(select,"B_Select_Diff_"+difficulty,"",94+difficulty*157,503,147,42);
                Image background=b.GetComponent<Image>();
                background.sprite=StyleSprite83("RowIdle83");
                background.color=Color.white;
                Text title=b.transform.Find("B_Select_Diff_"+difficulty+"_Label").GetComponent<Text>();
                title.fontSize=15;title.fontStyle=FontStyle.Bold;
                title.resizeTextForBestFit=true;
                title.resizeTextMinSize=11; title.resizeTextMaxSize=15;
                title.alignment=TextAnchor.MiddleCenter;
                SetRect((RectTransform)title.transform,5,1,137,40);
            }
            Label(select,"Select_DetailFooter","TRACK LENGTH  /  TAB TO SWITCH",96,548,465,20,12,
                new Color(.39f,.34f,.53f));
            // Separate duration selection; UI is still editable in Unity Inspector.
            for (int version = 0; version < 2; version++)
            {
                Button length=Btn(select,"B_Select_Length_"+version,"",94+version*235,572,220,36);
                Image plate=length.GetComponent<Image>();
                plate.sprite=StyleSprite83("RowIdle83");
                plate.color=Color.white;
                Text label=length.transform.Find("B_Select_Length_"+version+"_Label").GetComponent<Text>();
                label.fontSize=15; label.fontStyle=FontStyle.Bold;
                label.alignment=TextAnchor.MiddleCenter;
                label.resizeTextForBestFit=true;
                label.resizeTextMinSize=10; label.resizeTextMaxSize=15;
                SetRect((RectTransform)label.transform,5,0,210,36);
            }
            Action83(select,"B_Select_Action","PLAY","01",72,623,293,69,true);
            SmallAction83(select,"B_Select_Add","ADD SONG",369,635,196,52);
            // Back is at lower corner, away from the track card text.
            SmallAction83(select,"B_Select_Back","BACK",572,637,104,50);
            Image83(select,"Select_ListPanel","ListPanel83",622,104,614,520);
            Label(select,"Select_ListHeading","ALL TRACKS",656,119,346,39,26,Color.white);
            Label(select,"Select_Hint","SELECT  /  07 SHOWN",1005,123,189,29,13,
                new Color(.65f,.55f,.79f),TextAnchor.MiddleRight);
            for(int i=0;i<7;i++)
            {
                float rowY=168+i*62f;
                Button button=Btn(select,"B_Song_"+i,"",650,rowY,558,54);
                Image image=button.GetComponent<Image>();
                image.sprite=StyleSprite83("RowIdle83");image.color=Color.white;
                Image83(button.transform,"B_Song_"+i+"_Cover","Cover"+(i%13),19,6,43,42);
                var tx=button.transform.Find("B_Song_"+i+"_Label").GetComponent<Text>();
                tx.fontSize=17;tx.fontStyle=FontStyle.Bold;tx.alignment=TextAnchor.MiddleLeft;
                tx.color=Color.white;tx.resizeTextForBestFit=true;
                tx.resizeTextMinSize=12;tx.resizeTextMaxSize=17;
                SetRect((RectTransform)tx.transform,84,1,418,52);
                Label(button.transform,"B_Song_"+i+"_Arrow",">",514,1,32,52,20,
                    new Color(.75f,.62f,.90f),TextAnchor.MiddleCenter);
                Img(select,"Select_Accent_"+i,650,rowY+7,3,40,null,Color.clear);
            }
            SmallAction83(select,"B_Select_Up","PREV",915,635,129,51);
            SmallAction83(select,"B_Select_Down","NEXT",1052,635,137,51);
            Label(select,"Select_Status","",693,687,494,27,14,
                new Color(.9f,.71f,1f),TextAnchor.MiddleRight);
            select.gameObject.AddComponent<CanvasGroup>();
            select.gameObject.AddComponent<DuovexPageReveal>();
        }

        static InputField Input(Transform parent,string id,float x,float y,float w,float h)
        {
            Image bg=Img(parent,id,x,y,w,h,null,new Color(.10f,.15f,.24f));bg.raycastTarget=true;
            var input=bg.gameObject.AddComponent<InputField>();
            var text=Label(bg.transform,id+"_Text","",8,3,w-16,h-6,16,Color.white);
            input.textComponent=text;
            return input;
        }
        static UnityEngine.UI.Slider CreateSlider(Transform parent,string id,float x,float y,float w,float h)
        {
            Image bg=Img(parent,id,x,y,w,h,null,new Color(.12f,.22f,.32f)); bg.raycastTarget=true;
            var fill=Img(bg.transform,id+"_Fill",0,0,w,h,null,cyan);
            var handle=Img(bg.transform,id+"_Handle",0,-5,16,h+10,null,Color.white); handle.raycastTarget=true;
            var slider=bg.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.targetGraphic=handle;
            slider.fillRect=(RectTransform)fill.transform;slider.handleRect=(RectTransform)handle.transform;
            slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;
            return slider;
        }
        static void EnsureEvents()
        {
            if(UnityEngine.Object.FindAnyObjectByType<EventSystem>()!=null) return;
            var e=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        }
    }
}
