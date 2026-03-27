using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VEF.Genes;
using Verse;
using LudeonTK;
using TabulaRasa;

namespace DevToolBox
{
    [StaticConstructorOnStartup]
    public class Dialog_ResearchEditor : Window
    {
        public Vector2 windowPosition;

        public static List<DebugActionNode> cachedNodes;

        public int reorderableGroupID = -1;

        public Dictionary<string, string> nameCache = new Dictionary<string, string>();

        public int lastLabelCacheFrame = -1;

        public Vector2 defaultSize = new Vector2(600f, 380f);

        public Vector2 optionsScrollPosition;
        public float optionsViewRectHeight;

        public Dictionary<ResearchProjectDef, ResearchProjectChanges> researchOriginal = new Dictionary<ResearchProjectDef, ResearchProjectChanges>();
        public Dictionary<ResearchProjectDef, ResearchProjectChanges> researchChanges = new Dictionary<ResearchProjectDef, ResearchProjectChanges>();

        public ResearchProjectDef selectedResearch;
        public ResearchProjectChanges selectedResearchChanges;

        public Dialog_ResearchEditor()
        {
            resizeable = true;
            draggable = true;
            focusWhenOpened = false;
            drawShadow = false;
            closeOnAccept = false;
            closeOnCancel = false;
            preventCameraMotion = false;
            drawInScreenshotMode = false;
            windowPosition = DevToolBoxPrefs.GeneEditorPosition;
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
            DoResearchStuff(listing);
            // ======================================================================
            optionsViewRectHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();
            if (!Mathf.Approximately(windowRect.x, windowPosition.x) || !Mathf.Approximately(windowRect.y, windowPosition.y))
            {
                windowPosition = new Vector2(windowRect.x, windowRect.y);
                DevToolBoxPrefs.GeneEditorPosition = windowPosition;
            }
        }

        public string _drawScaleBuffer;
        public string _drawOffsetBufferX;
        public string _drawOffsetBufferY;
        public string _drawOffsetBufferZ;

        public void DoResearchStuff(Listing_Standard listing)
        {
            if (listing.ButtonDebug(selectedResearch?.label ?? "Select Research Project...", false))
            {
                Find.WindowStack.Add(new FloatMenu(GetAllResearch().ToList()));
            }
            listing.GapLine();
            if(selectedResearchChanges != null)
            {
                // Research Tab
                if (listing.ButtonTextLabeled("Research Tab", selectedResearchChanges.tab?.LabelCap ?? "Undefined"))
                {
                    Find.WindowStack.Add(new FloatMenu(GetResearchTabs().ToList()));
                }
                // Tech Level
                if (listing.ButtonTextLabeled("Tech Level", selectedResearchChanges.techLevel.ToStringSafe()))
                {
                    Find.WindowStack.Add(new FloatMenu(GetTechLevels().ToList()));
                }
                // Edit Cost
                listing.AddLabeledNumericalTextField("Base Cost:", ref selectedResearchChanges.baseCost, 0.5f, 50f, 100000f);
                // Position
                listing.AddLabeledNumericalTextField("Horizontal Position (X)", ref selectedResearchChanges.researchViewX, 0.5f, 0f, 1000f);
                listing.AddLabeledNumericalTextField("Vertical Position (Y)", ref selectedResearchChanges.researchViewY, 0.5f, 0f, 1000f);
                // Prerequisites
                listing.GapLine();
                if (selectedResearchChanges.prerequisites == null)
                {
                    listing.ButtonText("Enable Prerequisites");
                }
                else
                {
                    for (int i = 0; i < selectedResearchChanges.prerequisites.Count; i++)
                    {
                        listing.LabelBacked($"[{selectedResearchChanges.prerequisites[i].modContentPack?.Name ?? "???"}] {selectedResearchChanges.prerequisites[i].LabelCap}", Color.gray, GameFont.Small);
                    }
                    if (listing.ButtonText("Add Prerequisites..."))
                    {
                        Find.WindowStack.Add(new FloatMenu(GetPrerequisites(false).ToList()));
                    }
                    if (!selectedResearchChanges.prerequisites.NullOrEmpty() && listing.ButtonText("Remove Prerequisites..."))
                    {
                        Find.WindowStack.Add(new FloatMenu(GetPrerequisites(true).ToList()));
                    }
                }
            }
            // BUTTONS!
            if (!researchOriginal.NullOrEmpty() && listing.ButtonText("Apply All Changes"))
            {
                foreach (KeyValuePair<ResearchProjectDef, ResearchProjectChanges> kvp in researchChanges)
                {
                    ResearchProjectDef o = kvp.Key;
                    ResearchProjectChanges c = kvp.Value;
                    o.baseCost = c.baseCost;
                    o.techLevel = c.techLevel;
                    o.researchViewX = c.researchViewX;
                    o.researchViewY = c.researchViewY;
                    o.prerequisites = c.prerequisites;
                    {
                        o.x = o.researchViewX;
                        o.y = o.researchViewY;
                    }
                    ResearchProjectDef.ClampInCoordinateLimits(o);
                }
                MainTabWindow_Research window;
                if (Find.WindowStack.TryGetWindow<MainTabWindow_Research>(out window))
                {
                    if (window != null) { window.cachedVisibleResearchProjects = null; }
                }
            }
            if (!researchChanges.NullOrEmpty() && listing.ButtonText("Copy XML Changes"))
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<ResearchProjectDef, ResearchProjectChanges> kvp in researchChanges)
                {
                    ResearchProjectChanges o = researchOriginal[kvp.Key];
                    ResearchProjectChanges c = kvp.Value;
                    if (o.Equals(c)) { continue; }
                    sb.AppendLine($"[{kvp.Key.modContentPack.PackageId ?? "???"}] kvp.Key.defName");
                    if (o.baseCost != c.baseCost)
                    {
                        sb.AppendLine($"  <baseCost>{Mathf.RoundToInt(c.baseCost)}</baseCost>");
                    }
                    if (o.techLevel != c.techLevel)
                    {
                        sb.AppendLine($"  <techLevel>{c.techLevel}</techLevel>");
                    }
                    if (o.researchViewX != c.researchViewX)
                    {
                        sb.AppendLine($"  <researchViewX>{c.researchViewX:F2}</researchViewX>");
                    }
                    if (o.researchViewY != c.researchViewY)
                    {
                        sb.AppendLine($"  <researchViewY>{c.researchViewY:F2}</researchViewY>");
                    }
                    if (o.prerequisites != c.prerequisites)
                    {
                        if (c.prerequisites.NullOrEmpty()) { sb.AppendLine("  <prerequisites />"); }
                        else
                        {
                            sb.AppendLine("  <prerequisites>");
                            for (int i = 0; i < c.prerequisites.Count; i++)
                            {
                                sb.AppendLine($"    <li>{c.prerequisites[i].defName}</li>");
                            }
                            sb.AppendLine("  </prerequisites>");
                        }
                    }
                    sb.AppendLine();
                }
                GUIUtility.systemCopyBuffer = sb.ToString();
                Messages.Message("Modified data copied to clipboard.", MessageTypeDefOf.SituationResolved, historical: false);
            }
            if (!researchChanges.NullOrEmpty() && listing.ButtonText("Copy XML Patch"))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<Patch>");
                sb.AppendLine();
                foreach (KeyValuePair<ResearchProjectDef, ResearchProjectChanges> kvp in researchChanges)
                {
                    ResearchProjectChanges o = researchOriginal[kvp.Key];
                    ResearchProjectChanges c = kvp.Value;
                    if (c.Equals(o)) { continue; }
                    sb.AppendLine($"    <!-- ==================== {kvp.Key.LabelCap} ============================= -->");
                    sb.AppendLine();
                    sb.AppendLine("    <Operation Class=\"PatchOperationSequence\">");
                    sb.AppendLine("        <operations>");
                    {
                        if (o.tab != c.tab)
                        {
                            sb.AppendLine($"            <!-- ============================= Research Tab ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/tab</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/tab</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <tab>{c.tab}</tab>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <tab>{c.tab}</tab>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                        }
                        if (o.techLevel != c.techLevel)
                        {
                            sb.AppendLine($"            <!-- ============================= Tech Level ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/techLevel</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/techLevel</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <techLevel>{c.techLevel}</techLevel>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <techLevel>{c.techLevel}</techLevel>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                        }
                        if (o.baseCost != c.baseCost)
                        {
                            sb.AppendLine($"            <!-- ============================= Base Cost ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/baseCost</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/baseCost</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <baseCost>{c.baseCost}</baseCost>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match>");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <baseCost>{c.baseCost}</baseCost>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                        }
                        if (o.researchViewX != c.researchViewX)
                        {
                            sb.AppendLine($"            <!-- ============================= Horizontal Position ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/researchViewX</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/researchViewX</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <researchViewX>{c.researchViewX}</researchViewX>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match>");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <researchViewX>{c.researchViewX}</researchViewX>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                        }
                        if (o.researchViewY != c.researchViewY)
                        {
                            sb.AppendLine($"            <!-- ============================= Vertical Position ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/researchViewY</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/researchViewY</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <researchViewY>{c.researchViewY}</researchViewY>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match>");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            sb.AppendLine($"                        <researchViewY>{c.researchViewY}</researchViewY>");
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                        }
                        if (o.prerequisites != c.prerequisites)
                        {
                            sb.AppendLine($"            <!-- ============================= Vertical Position ============================= -->");
                            sb.AppendLine($"            <li Class=\"PatchOperationConditional\">");
                            sb.AppendLine($"                <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/prerequisites</xpath>");
                            sb.AppendLine($"                <match Class=\"PatchOperationReplace\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]/prerequisites</xpath>");
                            sb.AppendLine($"                    <value>");
                            DoPrerequisites();
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </match>");
                            sb.AppendLine($"                <nomatch Class=\"PatchOperationAdd\">");
                            sb.AppendLine($"                    <xpath>/Defs/ResearchProjectDef[defName=\"{kvp.Key.defName}\"]</xpath>");
                            sb.AppendLine($"                    <value>");
                            DoPrerequisites();
                            sb.AppendLine($"                    </value>");
                            sb.AppendLine($"                </nomatch>");
                            sb.AppendLine($"            </li>");
                            void DoPrerequisites()
                            {
                                if (c.prerequisites.NullOrEmpty())
                                {
                                    sb.AppendLine($"                        <prerequisites Inherit=\"False\"/>");
                                }
                                else
                                {
                                    sb.AppendLine($"                        <prerequisites>");
                                    for (int i = 0; i < c.prerequisites.Count; i++)
                                    {
                                        sb.AppendLine($"                            <li>{c.prerequisites[i].defName}</li>");
                                    }
                                    sb.AppendLine($"                        </prerequisites>");
                                }
                            }
                        }
                    }
                    sb.AppendLine("        </operations>");
                    sb.AppendLine("    </Operation>");
                    sb.AppendLine();
                }
                sb.AppendLine("</Patch>");
                GUIUtility.systemCopyBuffer = sb.ToString();
                Messages.Message("Patch data copied to clipboard.", MessageTypeDefOf.SituationResolved, historical: false);
            }
        }

        public IEnumerable<FloatMenuOption> GetPrerequisites(bool remove = false)
        {
            foreach (ResearchProjectDef proj in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (selectedResearch == proj) { continue; }
                if (remove && selectedResearchChanges.prerequisites.Contains(proj))
                {
                    yield return new FloatMenuOption($"Remove {proj.LabelCap}", delegate { selectedResearchChanges.prerequisites.Remove(proj); });
                }
                else if (!remove)
                {
                    yield return new FloatMenuOption($"Add {proj.LabelCap}", delegate { selectedResearchChanges.prerequisites.Add(proj); });
                }
            }
            yield break;
        }

        public IEnumerable<FloatMenuOption> GetResearchTabs()
        {
            foreach(ResearchTabDef tab in DefDatabase<ResearchTabDef>.AllDefs)
            {
                yield return new FloatMenuOption(tab.LabelCap, delegate { selectedResearchChanges.tab = tab; });
            }
            yield break;
        }

        public IEnumerable<FloatMenuOption> GetTechLevels()
        {
            yield return new FloatMenuOption("Undefined", delegate { selectedResearchChanges.techLevel = TechLevel.Undefined; });
            yield return new FloatMenuOption("Animal", delegate { selectedResearchChanges.techLevel = TechLevel.Animal; });
            yield return new FloatMenuOption("Neolithic", delegate { selectedResearchChanges.techLevel = TechLevel.Neolithic; });
            yield return new FloatMenuOption("Medieval", delegate { selectedResearchChanges.techLevel = TechLevel.Medieval; });
            yield return new FloatMenuOption("Industrial", delegate { selectedResearchChanges.techLevel = TechLevel.Industrial; });
            yield return new FloatMenuOption("Spacer", delegate { selectedResearchChanges.techLevel = TechLevel.Spacer; });
            yield return new FloatMenuOption("Ultra", delegate { selectedResearchChanges.techLevel = TechLevel.Ultra; });
            yield return new FloatMenuOption("Archotech", delegate { selectedResearchChanges.techLevel = TechLevel.Archotech; });
            yield break;
        }

        public IEnumerable<FloatMenuOption> GetAllResearch()
        {
            foreach(ResearchProjectDef proj in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (!researchOriginal.ContainsKey(proj))
                {
                    researchOriginal.Add(proj, new ResearchProjectChanges() {
                        tab = proj.tab,
                        baseCost = proj.baseCost, 
                        prerequisites = proj.prerequisites, 
                        researchViewX = proj.ResearchViewX, 
                        researchViewY = proj.researchViewY, 
                        techLevel = proj.techLevel,
                    });
                }
                if (!researchChanges.ContainsKey(proj))
                {
                    researchChanges.Add(proj, new ResearchProjectChanges()
                    {
                        tab = proj.tab,
                        baseCost = proj.baseCost,
                        prerequisites = proj.prerequisites,
                        researchViewX = proj.ResearchViewX,
                        researchViewY = proj.researchViewY,
                        techLevel = proj.techLevel,
                    });
                }
                yield return new FloatMenuOption($"[{proj.modContentPack?.Name ?? "???"}] {proj.label}", delegate { selectedResearch = proj; selectedResearchChanges = researchChanges[proj]; });
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

        public class ResearchProjectChanges
        {
            public ResearchTabDef tab;
            public float researchViewX;
            public float researchViewY;
            public float baseCost;
            public TechLevel techLevel;
            public List<ResearchProjectDef> prerequisites;

            public override bool Equals(object obj)
            {
                ResearchProjectChanges other = obj as ResearchProjectChanges;
                if (other == null) { return false; }
                if (tab != other.tab) { return false; }
                if (researchViewX != other.researchViewX) { return false; }
                if (researchViewY != other.researchViewY) { return false; }
                if (baseCost != other.baseCost) { return false; }
                if (techLevel != other.techLevel) { return false; }
                if (prerequisites != other.prerequisites) { return false; }
                return true;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
