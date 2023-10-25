using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VanillaGenesExpanded;
using Verse;
using TabulaRasa;

namespace GeneOffsetTool
{
    [StaticConstructorOnStartup]
    public class Dialog_GeneEditor : Window
    {
        public Vector2 windowPosition;

        public static List<DebugActionNode> cachedNodes;

        public int reorderableGroupID = -1;

        public Dictionary<string, string> nameCache = new Dictionary<string, string>();

        public int lastLabelCacheFrame = -1;

        public Vector2 defaultSize = new Vector2(600f, 380f);

        public Vector2 optionsScrollPosition;
        public float optionsViewRectHeight;

        public GeneDef selectedGene;

        public Dialog_GeneEditor()
        {
            resizeable = true;
            draggable = true;
            focusWhenOpened = false;
            drawShadow = false;
            closeOnAccept = false;
            closeOnCancel = false;
            preventCameraMotion = false;
            drawInScreenshotMode = false;
            windowPosition = Prefs.DevPalettePosition;
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
            DoGeneStuff(listing);
            // ======================================================================
            optionsViewRectHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();
        }

        public string _drawScaleBuffer;
        public string _drawOffsetBufferX;
        public string _drawOffsetBufferY;
        public string _drawOffsetBufferZ;

        public void DoGeneStuff(Listing_Standard listing)
        {
            if (listing.ButtonDebug(selectedGene?.label ?? "Select Gene...", false))
            {
                Find.WindowStack.Add(new FloatMenu(GetAllGenesWithGraphicsDropdown().ToList()));
            }
            listing.GapLine();
            if(selectedGene != null)
            {
                float parsedValue;
                listing.ValueLabeled("Draw Location", null, ref selectedGene.graphicData.drawLoc);
                listing.ValueLabeled("Display Layer", null, ref selectedGene.graphicData.layer);
                listing.ValueLabeled("Color Type", null, ref selectedGene.graphicData.colorType);

                if (_drawScaleBuffer.NullOrEmpty()) { _drawScaleBuffer = selectedGene.graphicData.drawScale.ToString(); }
                listing.AddLabeledTextField("Draw Scale", ref _drawScaleBuffer, 0.3f);
                if(float.TryParse(_drawScaleBuffer, out parsedValue)) { selectedGene.graphicData.drawScale = parsedValue; }

                GeneExtension geneExt = selectedGene.GetModExtension<GeneExtension>();
                if(geneExt == null || (geneExt.offsets.south == null && geneExt.offsets.north == null && geneExt.offsets.east == null && geneExt.offsets.west == null))
                {
                    listing.AddVector3TextFields("Draw Offset", ref selectedGene.graphicData.drawOffset.x, ref selectedGene.graphicData.drawOffset.y, ref selectedGene.graphicData.drawOffset.z);
                }
                if(geneExt != null)
                {
                    if(selectedGene.graphicData.fur != null)
                    {
                        listing.CheckboxLabeled("<useMaskForFur>", ref geneExt.useMaskForFur);
                        listing.CheckboxLabeled("<useSkinColorForFur>", ref geneExt.useSkinColorForFur);
                        listing.CheckboxLabeled("<dontColourFur>", ref geneExt.dontColourFur);
                        listing.CheckboxLabeled("<furHidesBody>", ref geneExt.furHidesBody);
                    }
                    DoPerBodyOffsets(listing, geneExt);
                }
                //if(listing.ButtonText("Clear Cached Data"))
                //{
                //    selectedGene.ClearCachedData();
                //}
            }
        }

        public void DoPerBodyOffsets(Listing_Standard listing, GeneExtension geneExt)
        {
            if (!geneExt.offsets?.south?.bodyTypes?.NullOrEmpty() ?? false)
            {
                listing.LabelBacked("South Offsets", Color.white);
                foreach (GeneExtension.BodyTypeOffset bodyOffset in geneExt.offsets.south.bodyTypes)
                {
                    listing.AddVector3TextFields(bodyOffset.bodyType.defName, ref bodyOffset.offset.x, ref bodyOffset.offset.y, ref bodyOffset.offset.z);
                }
            }
            if (!geneExt.offsets?.north?.bodyTypes?.NullOrEmpty() ?? false)
            {
                listing.LabelBacked("North Offsets", Color.white);
                foreach (GeneExtension.BodyTypeOffset bodyOffset in geneExt.offsets.north.bodyTypes)
                {
                    listing.AddVector3TextFields(bodyOffset.bodyType.defName, ref bodyOffset.offset.x, ref bodyOffset.offset.y, ref bodyOffset.offset.z);
                }
            }
            if (!geneExt.offsets?.east?.bodyTypes?.NullOrEmpty() ?? false)
            {
                listing.LabelBacked("East Offsets", Color.white);
                foreach (GeneExtension.BodyTypeOffset bodyOffset in geneExt.offsets.east.bodyTypes)
                {
                    listing.AddVector3TextFields(bodyOffset.bodyType.defName, ref bodyOffset.offset.x, ref bodyOffset.offset.y, ref bodyOffset.offset.z);
                }
            }
            if (!geneExt.offsets?.west?.bodyTypes?.NullOrEmpty() ?? false)
            {
                listing.LabelBacked("West Offsets", Color.white);
                foreach (GeneExtension.BodyTypeOffset bodyOffset in geneExt.offsets.west.bodyTypes)
                {
                    listing.AddVector3TextFields(bodyOffset.bodyType.defName, ref bodyOffset.offset.x, ref bodyOffset.offset.y, ref bodyOffset.offset.z);
                }
            }
        }

        public IEnumerable<FloatMenuOption> GetAllGenesWithGraphicsDropdown()
        {
            foreach(GeneDef gene in DefDatabase<GeneDef>.AllDefs)
            {
                if (gene.HasGraphic || gene.graphicData?.fur != null)
                {
                    yield return new FloatMenuOption(gene.label, delegate { selectedGene = gene; });
                }
            }
            yield break;
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
