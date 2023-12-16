using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using TabulaRasa;

namespace DevToolBox
{
    [StaticConstructorOnStartup]
    public class Dialog_BuildingEditor : Window
    {
        public Vector2 windowPosition;

        public static List<DebugActionNode> cachedNodes;

        public int reorderableGroupID = -1;

        public Dictionary<string, string> nameCache = new Dictionary<string, string>();

        public int lastLabelCacheFrame = -1;

        public Vector2 defaultSize = new Vector2(600f, 380f);

        public Vector2 optionsScrollPosition;
        public float optionsViewRectHeight;

        public Dialog_BuildingEditor()
        {
            resizeable = true;
            draggable = true;
            focusWhenOpened = false;
            drawShadow = false;
            closeOnAccept = false;
            closeOnCancel = false;
            preventCameraMotion = false;
            drawInScreenshotMode = false;
            windowPosition = DevToolBoxPrefs.BuildingEditorPosition;
            onlyDrawInDevMode = true;
            lastLabelCacheFrame = RealTime.frameCount;
        }

        public override void DoWindowContents(Rect inRect)
        {
            bool flag = optionsViewRectHeight > inRect.height;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width - (flag ? 26f : 0f), optionsViewRectHeight);
            Widgets.BeginScrollView(inRect, ref optionsScrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            Rect rect = new Rect(viewRect.x, viewRect.y, viewRect.width, 999999f);
            listing.Begin(rect);
            // ============================ CONTENTS ================================
            DoBuildingStuff(listing);
            // ======================================================================
            optionsViewRectHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();
            if (!Mathf.Approximately(windowRect.x, windowPosition.x) || !Mathf.Approximately(windowRect.y, windowPosition.y))
            {
                windowPosition = new Vector2(windowRect.x, windowRect.y);
                DevToolBoxPrefs.BuildingEditorPosition = windowPosition;
            }
        }

        public void DoBuildingStuff(Listing_Standard listing)
        {
            if (Find.Selector?.SingleSelectedThing is Building)
            {
                Building selBuilding = (Building)Find.Selector.SingleSelectedThing;
                listing.Label("Selected Building: " + selBuilding.LabelCap);
                listing.GapLine();
                listing.LabelBacked("Graphic Data", Color.white);
                listing.AddVector2TextFields("Draw Size", ref selBuilding.def.graphicData.drawSize.x, ref selBuilding.def.graphicData.drawSize.y, -999f, 999f);
                listing.AddVector3TextFields("Draw Offset", ref selBuilding.def.graphicData.drawOffset.x, ref selBuilding.def.graphicData.drawOffset.y, ref selBuilding.def.graphicData.drawOffset.z, -999f, 999f);
                if (listing.ButtonText("Recache Graphics"))
                {
                    selBuilding.def.graphicData.cachedGraphic = null;
                    selBuilding.graphicInt = null;
                    selBuilding.styleGraphicInt = null;
                    if (selBuilding.Spawned)
                    {
                        selBuilding.Map.mapDrawer.MapMeshDirty(selBuilding.Position, MapMeshFlag.Things);
                    }
                }
            }
            else
            {
                listing.Label("No Building Selected");
                listing.GapLine();
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (RealTime.frameCount >= lastLabelCacheFrame + 30)
            {
                nameCache.Clear();
                lastLabelCacheFrame = RealTime.frameCount;
            }
        }

        public override void SetInitialSizeAndPosition()
        {
            windowPosition.x = Mathf.Clamp(windowPosition.x, 0f, UI.screenWidth - defaultSize.x);
            windowPosition.y = Mathf.Clamp(windowPosition.y, 0f, UI.screenHeight - defaultSize.y);
            windowRect = new Rect(windowPosition.x, windowPosition.y, defaultSize.x, defaultSize.y);
            windowRect = windowRect.Rounded();
        }
    }
}
