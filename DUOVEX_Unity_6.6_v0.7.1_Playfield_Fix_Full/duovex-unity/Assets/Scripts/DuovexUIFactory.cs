using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

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
        [Range(8f,40f)] public float noteHeight = 18f;
        [Range(0f,18f)] public float noteSideInset = 4f;
    }

    // All static UI is ordinary Canvas/Image/Text/Button geometry, not an OnGUI surface.
    // Editor menu Tools > Duovex > Rebuild Editable Scene persists this hierarchy to a scene.
    public static class DuovexUIFactory
    {
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
            Img(design, "Background", 0, 0, 1280, 720, theme != null ? theme.background : null,
                Color.white);
            Img(design, "TopBar", 0, 0, 1280, 53, null, new Color(.017f, .027f, .055f, .87f));
            Img(design, "TopAccent", 0, 51, 1280, 2, null, new Color(.30f, .70f, .91f, .38f));
            Label(design, "Brand", "DUOVEX", 57, 12, 240, 30, 22, cyan, TextAnchor.MiddleLeft);
            Img(design, "BrandMark", 20, 15, 30, 30, theme != null ? theme.duovexMark : null, Color.white);
            Label(design, "HeaderEdition", "// RHYTHM", 1085, 14, 160, 24, 13, new Color(.42f,.53f,.69f),TextAnchor.MiddleRight);

            // Main composition: typography on the left; editable orbital/key art on the right.
            var main = Group(design, "Page_Main");
            Img(main, "Main_Halo", 688, 49, 542, 607, theme != null ? theme.glow : null,
                new Color(.58f,.60f,1f,.47f));
            var orbital=Img(main, "Main_Orbital", 665, 48, 566, 590, theme != null ? theme.menuOrbital : null, Color.white);
            // Center pivot makes subtle rotation orbit in place, not across the screen.
            RectTransform orbitalRect=(RectTransform)orbital.transform;
            orbitalRect.pivot=new Vector2(.5f,.5f);
            orbitalRect.anchoredPosition=new Vector2(665+283,-(48+295));
            Img(main, "Main_Lane_1", 787, 135, 91, 415, theme != null ? theme.lane : null, new Color(.72f,.86f,1f,.65f));
            Img(main, "Main_Lane_2", 884, 135, 91, 415, theme != null ? theme.lane : null, new Color(.88f,.78f,1f,.88f));
            Img(main, "Main_Lane_3", 981, 135, 91, 415, theme != null ? theme.lane : null, new Color(.72f,.86f,1f,.65f));
            Img(main, "Main_Scan", 787, 434, 285, 5, theme != null ? theme.hitLine : null, Color.white);
            Img(main, "Main_Glyph", 855, 267, 152, 152, theme != null ? theme.duovexMark : null,
                new Color(.95f,.98f,1f,.86f));
            Label(main, "Main_Kicker", "RHYTHM  /  REDEFINED", 99, 170, 500, 24, 14,
                new Color(.52f,.67f,.83f));
            Label(main, "Main_Logo", "DUOVEX", 90, 205, 590, 106, 91, Color.white, TextAnchor.MiddleLeft);
            Img(main, "Main_Divider", 100, 327, 468, 3, theme != null ? theme.hitLine : null, Color.white);
            Label(main, "Main_Subtitle", "TWO HANDS. ONE SHARED LANE.", 99, 346, 522, 28, 16,
                new Color(.49f,.61f,.78f));
            Btn(main, "B_Main_Play", "PLAY", 99, 416, 399, 65, true);
            Btn(main, "B_Main_Edit", "CHART STUDIO", 99, 497, 399, 54);
            Btn(main, "B_Main_Options", "OPTIONS", 99, 567, 399, 54);

            var select = Group(design, "Page_Select");
            Label(select, "Select_Header", "SELECT TRACK", 71, 84, 710, 53, 34, cyan);
            Img(select, "Select_ListPanel", 73, 152, 709, 491, theme != null ? theme.panel : null, panel);
            Img(select, "Select_InfoPanel", 800, 152, 412, 491, theme != null ? theme.panel : null, panel);
            for (int i = 0; i < 7; i++)
            {
                var songButton=Btn(select, "B_Song_"+i, "", 91, 180 + i*57, 671, 48);
                var title=songButton.transform.Find("B_Song_"+i+"_Label") as RectTransform;
                if(title!=null)
                {
                    SetRect(title,25,0,620,48);
                    title.GetComponent<Text>().alignment=TextAnchor.MiddleLeft;
                }
                Img(select, "Select_Accent_"+i, 92, 180+i*57, 4, 48, null, cyan);
            }
            Btn(select, "B_Select_Up", "UP", 96, 597, 125, 32);
            Btn(select, "B_Select_Down", "DOWN", 231, 597, 125, 32);
            Label(select, "Select_Title", "TUTORIAL", 821, 195, 361, 96, 28, Color.white);
            Label(select, "Select_Meta", "BPM 110", 823, 305, 351, 34, 15, theme != null ? theme.muted : Color.gray);
            Btn(select, "B_Select_Action", "PLAY", 823, 392, 356, 56, true);
            Btn(select, "B_Select_Add", "ADD SONG", 823, 465, 356, 56);
            Btn(select, "B_Select_Back", "BACK", 823, 542, 356, 56);
            Label(select, "Select_Status", "", 822, 601, 357, 25, 12, cyan);

            var options = Group(design, "Page_Options");
            Label(options, "Options_Header", "OPTIONS", 100, 95, 460, 49, 33, cyan);
            Img(options, "Options_Panel", 100, 190, 1080, 406, theme != null ? theme.panel : null, panel);
            Label(options, "Options_SFX", "HIT SOUND", 153, 240, 280, 38, 24, Color.white);
            var sound = CreateSlider(options, "Slider_SFX", 153, 323, 570, 29);
            sound.value = .32f;
            Label(options, "Options_Volume", "32%", 756, 322, 110, 35, 20, cyan);
            Btn(options, "B_White_Preview", "WHITE PREVIEW", 154, 396, 235, 48);
            Btn(options, "B_Black_Preview", "BLACK PREVIEW", 402, 396, 235, 48);
            Label(options, "Options_Speed", "SCROLL SPEED", 154, 478, 275, 38, 19, Color.white);
            Btn(options, "B_Speed_Minus", "-", 483, 480, 50, 41);
            Label(options, "Options_SpeedValue", "1.0x", 548, 480, 100, 41, 23, cyan, TextAnchor.MiddleCenter);
            Btn(options, "B_Speed_Plus", "+", 670, 480, 50, 41);
            Btn(options, "B_Options_Back", "BACK", 935, 529, 209, 48);

            var result = Group(design, "Page_Result");
            Img(result, "Result_Panel", 220, 107, 840, 490, theme != null ? theme.panel : null, panel);
            Img(result,"Result_Ornament",874,145,122,122,theme != null ? theme.duovexMark : null,
                new Color(.82f,.91f,1f,.32f));
            Label(result, "Result_Heading", "TRACK COMPLETE", 261, 140, 720, 58, 35, cyan);
            Img(result,"Result_Divider",263,214,749,3,theme != null ? theme.hitLine : null,Color.white);
            Label(result,"Result_AccLabel","ACCURACY",266,247,350,27,14,new Color(.52f,.64f,.80f));
            Label(result, "Result_Accuracy", "00.00%", 264, 273, 480, 88, 62, Color.white);
            Label(result, "Result_Summary", "MAX COMBO   0\nMISS   0\nSCORE   0", 787, 257, 233, 180, 23, Color.white);
            Label(result, "Result_Breakdown", "", 264, 428, 750, 55, 15, theme != null ? theme.muted : Color.gray);
            Btn(result, "B_Result_Retry", "RETRY", 264, 511, 224, 56, true);
            Btn(result, "B_Result_Select", "TRACKS", 500, 511, 225, 56);
            Btn(result, "B_Result_Main", "MENU", 739, 511, 271, 56);

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
            Img(game,"Game_AmbientScrim",0,53,1280,615,null,new Color(.004f,.009f,.024f,.27f));
            Img(game,"Game_GearHalo",409,59,460,590,theme != null ? theme.glow : null,
                new Color(.35f,.58f,.92f,.18f));
            Img(game,"Game_GearPlate",446,69,388,571,theme != null ? theme.panel : null,
                new Color(.77f,.91f,1f,.72f));
            BuildPlayfield(game, "Game", 460, 78, 120, 586, true);
            Label(game,"Game_Header","CURRENT TRACK",62,105,334,26,13,new Color(.54f,.65f,.78f));
            Label(game,"Game_Track","",62,135,345,68,23,Color.white);
            Label(game,"Game_AccuracyLabel","ACCURACY",62,237,330,29,13,new Color(.52f,.64f,.80f));
            Label(game,"Game_Accuracy","",62,264,350,77,48,cyan);
            Label(game,"Game_ComboLabel","COMBO",884,233,304,25,14,new Color(.52f,.64f,.80f));
            Label(game,"Game_Combo","",882,258,326,80,56,Color.white);
            Label(game,"Game_Score","",882,340,325,40,19,Color.white);
            Label(game,"Game_Speed","",882,405,315,30,15,cyan);
            Label(game,"Game_Fast","",463,399,350,25,13,new Color(.65f,.86f,1f),TextAnchor.MiddleCenter);
            Label(game,"Game_Judge","",463,426,350,47,32,Color.white,TextAnchor.MiddleCenter);
            Label(game,"Game_Slow","",463,468,350,25,13,new Color(1f,.70f,.76f),TextAnchor.MiddleCenter);
            Label(game,"Game_Guide","",64,493,326,84,18,Color.white);
            Img(game,"Game_ProgressBack",51,657,1178,3,null,new Color(.19f,.25f,.4f));
            Img(game,"Game_Progress",51,657,0,3,theme != null ? theme.hitLine : null,cyan);
            Label(game,"Game_Time","",975,658,285,30,13,theme != null ? theme.muted : Color.gray);
            Img(game,"Game_PausePanel",455,256,370,151,theme != null ? theme.panel : null,panel);
            Label(game,"Game_Paused","PAUSED",472,270,339,89,41,cyan,TextAnchor.MiddleCenter);

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
            Img(page,prefix+"_HitGlow",x-7,hitY-13,laneW*3+14,26,
                theme!=null?theme.glow:null,new Color(.44f,.78f,1f,.35f));
            // Both the note center and the bright center line use exactly hitY.
            Img(page,prefix+"_HitLine",x,hitY-4,laneW*3,8,
                theme!=null?theme.hitLine:null,Color.white);
            if(gameplay)
            {
                string[] names={"D","F","J","K"};
                for(int i=0;i<4;i++)
                {
                    float px=i==0?x:i==1?x+laneW:i==2?x+laneW*1.5f:x+laneW*2f;
                    float w=i==0||i==3?laneW:laneW*.5f;
                    Img(page,"Game_KeyBG_"+i,px+2,hitY+9,w-4,30,theme!=null?theme.button:null,
                        i<2?new Color(.24f,.56f,.76f,.72f):new Color(.48f,.36f,.72f,.72f));
                    Label(page,"Game_Key_"+i,names[i],px+2,hitY+10,w-4,28,16,Color.white,TextAnchor.MiddleCenter);
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
            t.alignment=align; t.raycastTarget=false;
            return t;
        }
        static Button Btn(Transform parent,string id,string label,float x,float y,float w,float h,bool primary=false)
        {
            Image img=Img(parent,id,x,y,w,h,theme!=null?theme.button:null,primary?new Color(.50f,.93f,1f):panelAlt);
            img.raycastTarget=true;
            var b=img.gameObject.AddComponent<Button>(); b.targetGraphic=img;
            var colors=b.colors; colors.highlightedColor=new Color(1f,1f,1f);
            colors.pressedColor=new Color(.54f,.77f,.90f);
            colors.selectedColor=new Color(.86f,.95f,1f);
            colors.fadeDuration=.12f; b.colors=colors;
            Label(img.transform,id+"_Label",label,0,0,w,h,17,primary?new Color(.04f,.11f,.18f):new Color(.98f,.99f,1f),TextAnchor.MiddleCenter);
            return b;
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
