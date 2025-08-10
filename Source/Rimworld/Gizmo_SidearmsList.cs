using System;
using System.Collections.Generic;
using System.Linq;
using MySidearms.Compat;
using MySidearms.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static MySidearms.Utilities.Enums;
using static MySidearms.MySidearms;

namespace MySidearms.Rimworld
{
    public class Gizmo_SidearmsList : Command
    {
        public const float ContentPadding = 2f;
        public const float MinGizmoSize = 75f;
        public const float IconSize = 32f;
        public const float IconGap = 1f;
        public const float SelectorPanelWidth = 32f + ContentPadding * 2;
        public const float PreferenceIconHeight = 21f;
        public const float PreferenceIconWidth = 32f;

        public const float FirstTimeSettingsWarningWidth = 16f;


        public static readonly Color iconBaseColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color iconMouseOverColor = new Color(0.6f, 0.6f, 0.4f, 1f);

        public static readonly Color preferenceBase = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color preferenceSet = new Color(0.5f, 1.0f, 0.5f, 1f);
        public static readonly Color preferenceOfSkill = new Color(1.0f, 0.75f, 0.5f, 1f);
        public static readonly Color preferenceHighlight = new Color(0.7f, 0.7f, 0.4f, 1f);
        public static readonly Color preferenceHighlightSet = new Color(0.7f, 1.0f, 0.4f, 1f);

        //public Texture2D[] iconTextures;
        public Action hotkeyAction;
        CompSidearmMemory pawnMemory;

        public Pawn parent;
        public List<ThingWithComps> carriedWeapons;
        public List<ThingWithComps> carriedRangedWeapons;
        public List<ThingWithComps> carriedMeleeWeapons;

        public List<ThingDefStuffDefPair> weaponMemories;
        public List<ThingDefStuffDefPair> rangedWeaponMemories;
        public List<ThingDefStuffDefPair> meleeWeaponMemories;

        public enum SidearmsListInteraction
        {
            None,
            SelectorRanged,
            SelectorSkill,
            SelectorMelee,
            Weapon,
            UnmemorisedWeapon,
            WeaponMemory,
            Unarmed
        }
        public SidearmsListInteraction interactedWith = SidearmsListInteraction.None;
        public ThingWithComps interactionWeapon;
        public ThingDefStuffDefPair? interactionWeaponType;
        public bool interactionAsOffhand;
        public bool interactionWeaponIsDuplicate;

        public static float lastFrameWidth = 0f;

        public override float GetWidth(float maxWidth)
        {
            return lastFrameWidth;
        }
        
        /*public override float GetWidth(float maxWidth)
        {
            if (pawnMemory == null)
                return 75;
            int biggerCount = Math.Max(
                carriedRangedWeapons.Count + countMissingRangedWeapons(pawnMemory, parent),
                carriedMeleeWeapons.Count + countMissingMeleeWeapons(pawnMemory, parent) + 1
                );
            float width = SelectorPanelWidth + ContentPadding + (IconSize * biggerCount) + IconGap * (biggerCount - 1) + ContentPadding;
            if (!Settings.SettingsEverOpened)
                width += (FirstTimeSettingsWarningWidth + 2);
            return Math.Min(Math.Max(width, MinGizmoSize), maxWidth);
        }*/

        [Obsolete] //remains as VFECore uses this
        public Gizmo_SidearmsList(Pawn parent, IEnumerable<ThingWithComps> carriedWeapons, IEnumerable<ThingDefStuffDefPair> weaponMemories)
            : this(parent, carriedWeapons.ToList(), weaponMemories.ToList(), CompSidearmMemory.GetMemoryCompForPawn(parent)) { }

        public Gizmo_SidearmsList(Pawn parent, List<ThingWithComps> carriedWeapons, List<ThingDefStuffDefPair> weaponMemories, CompSidearmMemory pawnMemory)
        {
            this.parent = parent;

            this.pawnMemory = pawnMemory;
            
            this.carriedWeapons = carriedWeapons;
            this.carriedRangedWeapons = new List<ThingWithComps>();
            this.carriedMeleeWeapons = new List<ThingWithComps>();
            for (int i = carriedWeapons.Count - 1; i >= 0; i--)
            {
                var tmp = carriedWeapons[i];
                if (tmp.def.IsRangedWeapon) carriedRangedWeapons.Add(tmp);
                else if (tmp.def.IsMeleeWeapon) carriedMeleeWeapons.Add(tmp);
            }
            this.weaponMemories = weaponMemories;
            this.rangedWeaponMemories = new List<ThingDefStuffDefPair>();
            this.meleeWeaponMemories = new List<ThingDefStuffDefPair>();
            for (int i = weaponMemories.Count - 1; i >= 0; i--)
            {
                var tmp = weaponMemories[i];
                if (tmp.thing.IsRangedWeapon) rangedWeaponMemories.Add(tmp);
                else if (tmp.thing.IsMeleeWeapon) meleeWeaponMemories.Add(tmp);
            }
            tutorTag = "SidearmsList";
            this.defaultLabel = "DrawSidearm_gizmoTitle".Translate();
            this.defaultDesc = "DrawSidearm_gizmoTooltip".Translate();
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            return GizmoOnGUI_New(topLeft, maxWidth, parms);
        }

        public GizmoResult GizmoOnGUI_New(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var gizmoRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), MinGizmoSize);

            Widgets.DrawWindowBackground(gizmoRect);
            var contentRect = gizmoRect.ContractedBy(ContentPadding);

            Rect selectorPanel = new Rect(gizmoRect.x + ContentPadding, gizmoRect.y + ContentPadding, SelectorPanelWidth - ContentPadding * 2, MinGizmoSize - ContentPadding * 2);
            DrawPreferenceSelector(parent, pawnMemory, selectorPanel);

            var widthRanged = DrawRangedList(contentRect);
            var widthMelee = DrawMeleeList(contentRect);

            UIHighlighter.HighlightOpportunity(gizmoRect, "SidearmList");

            if (!Settings.SettingsEverOpened)
            {
                Rect position = new Rect((gizmoRect.x + gizmoRect.width - (FirstTimeSettingsWarningWidth + 2)), gizmoRect.y + 4, FirstTimeSettingsWarningWidth, FirstTimeSettingsWarningWidth);
                float brightness = Pulser.PulseBrightness(1f, 0.5f);
                GUI.color = new Color(brightness, brightness, 0f);
                GUI.DrawTexture(position, TextureResources.FirstTimeSettingsWarningIcon);
                if (Widgets.ButtonInvisible(position))
                {
                    var dialog = new Dialog_ModSettings(ModSingleton);
                    Find.WindowStack.Add(dialog);
                }
                TooltipHandler.TipRegion(position, "FirstTimeSettingsWarning".Translate());
            }

            GUI.color = Color.white;

            if (parent.IsColonistPlayerControlled)
                DrawGizmoLabel(defaultLabel, gizmoRect);
            else
                DrawGizmoLabel(defaultLabel + " (godmode)", gizmoRect);

            lastFrameWidth = Math.Max(widthRanged, widthMelee) + selectorPanel.width + ContentPadding * 4;
            if (!Settings.SettingsEverOpened)
                lastFrameWidth += (FirstTimeSettingsWarningWidth + 2);

            return interactedWith != SidearmsListInteraction.None ? new GizmoResult(GizmoState.Interacted, Event.current) : new GizmoResult(GizmoState.Clear);
        }

        public float DrawRangedList(Rect contentRect) 
        {
            carriedRangedWeapons = carriedRangedWeapons
                .OrderByDescending(t => t == parent.equipment.Primary ? 2 : ((Tacticowl.active && Tacticowl.isOffHand(t)) ? 1 : 0))
                .ThenByDescending(t => t.MarketValue).ToList();
            var unsatisfiedRangedMemories = new List<ThingDefStuffDefPair>(rangedWeaponMemories);

            var found = new HashSet<ThingDefStuffDefPair>();

            int pos = 0;
            for (; pos < carriedRangedWeapons.Count; pos++)
            {
                var weapon = carriedRangedWeapons[pos];
                ThingDefStuffDefPair weaponMemory = weapon.toThingDefStuffDefPair();
                bool isDupe = found.Contains(weaponMemory);
                found.Add(weaponMemory);

                var iconPos = new Vector2((IconSize * pos) + IconGap * (pos - 1) + SelectorPanelWidth, 0);
                DrawIconForWeapon(parent, pawnMemory, weapon, unsatisfiedRangedMemories, isDupe, contentRect, iconPos);
            }

            if(unsatisfiedRangedMemories.Count > 0) 
            {
                unsatisfiedRangedMemories.SortStable((a, b) => { return (int)((b.thing.BaseMarketValue - a.thing.BaseMarketValue) * 1000); });
                while (unsatisfiedRangedMemories.Any())
                {
                    var entry = unsatisfiedRangedMemories[0];
                    var remainingCount = unsatisfiedRangedMemories.Count(m => m == entry);

                    var iconPos = new Vector2((IconSize * pos) + IconGap * (pos - 1) + SelectorPanelWidth, 0);
                    DrawIconForWeaponMemory(parent, pawnMemory, entry, remainingCount, found.Contains(entry), contentRect, iconPos);
                    unsatisfiedRangedMemories.RemoveAll(m => m == entry);
                    pos++;
                }
            }

            return (pos * IconSize + IconGap) - IconGap;
        }

        public float DrawMeleeList(Rect contentRect) 
        {
            carriedMeleeWeapons = carriedMeleeWeapons
                .OrderByDescending(t => t == parent.equipment.Primary ? 2 : ((Tacticowl.active && Tacticowl.isOffHand(t)) ? 1 : 0))
                .ThenByDescending(t => t.MarketValue).ToList();
            var unsatisfiedMeleeMemories = new List<ThingDefStuffDefPair>(meleeWeaponMemories);

            var found = new HashSet<ThingDefStuffDefPair>();

            int pos = 0;
            for (; pos < carriedMeleeWeapons.Count; pos++)
            {
                var weapon = carriedMeleeWeapons[pos];
                ThingDefStuffDefPair weaponMemory = weapon.toThingDefStuffDefPair();
                bool isDupe = found.Contains(weaponMemory);
                found.Add(weaponMemory);

                var iconPos = new Vector2((IconSize * pos) + IconGap * (pos - 1) + SelectorPanelWidth, IconSize + IconGap);
                DrawIconForWeapon(parent, pawnMemory, weapon, unsatisfiedMeleeMemories, isDupe, contentRect, iconPos);
            }

            if (unsatisfiedMeleeMemories.Count > 0)
            {
                unsatisfiedMeleeMemories.SortStable((a, b) => { return (int)((b.thing.BaseMarketValue - a.thing.BaseMarketValue) * 1000); });
                while (unsatisfiedMeleeMemories.Any())
                {
                    var entry = unsatisfiedMeleeMemories[0];
                    var remainingCount = unsatisfiedMeleeMemories.Count(m => m == entry);

                    var iconPos = new Vector2((IconSize * pos) + IconGap * (pos - 1) + SelectorPanelWidth, IconSize + IconGap);
                    DrawIconForWeaponMemory(parent, pawnMemory, entry, remainingCount, found.Contains(entry), contentRect, iconPos);
                    unsatisfiedMeleeMemories.RemoveAll(m => m == entry);
                    pos++;
                }
            }

            var unarmedIconOffset = new Vector2((IconSize * pos) + (IconGap * (pos - 1)) + SelectorPanelWidth, IconSize + IconGap);
            DrawIconForUnarmed(parent, pawnMemory, contentRect, unarmedIconOffset);
            pos++;

            return (pos * IconSize) + (IconGap * (pos - 1));
        }
		
        public void DrawPreferenceSelector(Pawn pawn, CompSidearmMemory pawnMemory, Rect contentRect)
        {
            var rangedIconRect = new Rect(contentRect.x, contentRect.y, PreferenceIconWidth, PreferenceIconHeight);
            var skillIconRect = new Rect(contentRect.x, contentRect.y+ PreferenceIconHeight + IconGap, PreferenceIconWidth, PreferenceIconHeight);
            var meleeIconRect = new Rect(contentRect.x, contentRect.y+ (PreferenceIconHeight + IconGap) * 2, PreferenceIconWidth, PreferenceIconHeight);

            var skillPref = pawn.getSkillWeaponPreference();

            if (Mouse.IsOver(rangedIconRect))
            {
                if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.Ranged)
                    GUI.color = preferenceHighlightSet;
                else
                    GUI.color = preferenceHighlight;
                GUI.DrawTexture(rangedIconRect, TextureResources.preferRanged);
                TooltipHandler.TipRegion(rangedIconRect, string.Format("SidearmPreference_Ranged".Translate()));
                MouseoverSounds.DoRegion(rangedIconRect, SoundDefOf.Mouseover_Command);
            }
            else
            {
                if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.Ranged)
                    GUI.color = preferenceSet;
                else if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.BySkill && skillPref == PrimaryWeaponMode.Ranged)
                    GUI.color = preferenceOfSkill;
                else
                    GUI.color = preferenceBase;
                GUI.DrawTexture(rangedIconRect, TextureResources.preferRanged);
            }

            if (Mouse.IsOver(skillIconRect))
            {
                if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.BySkill)
                    GUI.color = preferenceHighlightSet;
                else
                    GUI.color = preferenceHighlight;
                GUI.DrawTexture(skillIconRect, TextureResources.preferSkilled);
                TooltipHandler.TipRegion(skillIconRect, string.Format("SidearmPreference_Skill".Translate()));
                MouseoverSounds.DoRegion(skillIconRect, SoundDefOf.Mouseover_Command);
            }
            else
            {
                if (pawn.skills != null)
                {
                    if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.BySkill)
                        GUI.color = preferenceSet;
                    else
                        GUI.color = preferenceBase;
                    GUI.DrawTexture(skillIconRect, TextureResources.preferSkilled);
                }
            }

            if (Mouse.IsOver(meleeIconRect))
            {
                if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.Melee)
                    GUI.color = preferenceHighlightSet;
                else
                    GUI.color = preferenceHighlight;
                GUI.DrawTexture(meleeIconRect, TextureResources.preferMelee);
                TooltipHandler.TipRegion(meleeIconRect, string.Format("SidearmPreference_Melee".Translate()));
                MouseoverSounds.DoRegion(meleeIconRect, SoundDefOf.Mouseover_Command);
            }
            else
            {
                if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.Melee)
                    GUI.color = preferenceSet;
                else if (pawnMemory.primaryWeaponMode == PrimaryWeaponMode.BySkill && skillPref == PrimaryWeaponMode.Melee)
                    GUI.color = preferenceOfSkill;
                else
                    GUI.color = preferenceBase;
                GUI.DrawTexture(meleeIconRect, TextureResources.preferMelee);
            }

            UIHighlighter.HighlightOpportunity(rangedIconRect, "SidearmPreferenceButton");
            UIHighlighter.HighlightOpportunity(skillIconRect, "SidearmPreferenceButton");
            UIHighlighter.HighlightOpportunity(meleeIconRect, "SidearmPreferenceButton");

            if (Widgets.ButtonInvisible(rangedIconRect, true))
            {
                interactedWith = SidearmsListInteraction.SelectorRanged;
            }
            if (Widgets.ButtonInvisible(skillIconRect, true))
            {
                interactedWith = SidearmsListInteraction.SelectorSkill;
            }
            if (Widgets.ButtonInvisible(meleeIconRect, true))
            {
                interactedWith = SidearmsListInteraction.SelectorMelee;
            }
        }

        public void DrawIconForWeaponMemory(Pawn pawn, CompSidearmMemory pawnMemory, ThingDefStuffDefPair weaponType, int stackCount, bool isDuplicate, Rect contentRect, Vector2 iconOffset)
        {
            Graphic g = weaponType.thing.graphicData.Graphic;

            var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, IconSize, IconSize);

            Texture2D drawPocket;
            drawPocket = TextureResources.drawPocketMemory;

            if (pawn.Drafted)
            {
                TooltipHandler.TipRegion(iconRect, string.Format("DrawSidearm_gizmoTooltipMemoryWhileDrafted".Translate(), weaponType.getLabel()));
            }
            else
            {
                TooltipHandler.TipRegion(iconRect, string.Format("DrawSidearm_gizmoTooltipMemory".Translate(), weaponType.getLabel()));
            }
            MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);
            if (Mouse.IsOver(iconRect))
            {
                GUI.color = iconMouseOverColor;
                GUI.DrawTexture(iconRect, drawPocket);
            }
            else
            {
                GUI.color = iconBaseColor;
                GUI.DrawTexture(iconRect, drawPocket);
            }

            Graphic outerGraphic = weaponType.thing.graphic;
            if (outerGraphic is Graphic_StackCount)
                outerGraphic = (outerGraphic as Graphic_StackCount).SubGraphicForStackCount(stackCount, weaponType.thing);

            Material material = outerGraphic.ExtractInnerGraphicFor(null).MatAt(weaponType.thing.defaultPlacingRot, null);
            Texture resolvedIcon = (Texture2D)material.mainTexture;
            GUI.color = weaponType.getDrawColor();
            GUI.DrawTexture(iconRect, resolvedIcon);
            GUI.color = Color.white;

            if (stackCount > 1)
            {
                var store = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(iconRect, stackCount.ToString());
                Text.Anchor = store;
            }

            if (!isDuplicate)
            {
                GUI.color = Color.white;

                if (pawnMemory.ForcedWeapon == weaponType)
                    GUI.DrawTexture(iconRect, TextureResources.forcedAlways);
                
                if(weaponType.thing.IsRangedWeapon & pawnMemory.DefaultRangedWeapon == weaponType)
                    GUI.DrawTexture(iconRect, TextureResources.defaultRanged);
                else if (pawnMemory.PreferredMeleeWeapon == weaponType)
                    GUI.DrawTexture(iconRect, TextureResources.preferredMelee);

                GUI.color = Color.white;
            }

            UIHighlighter.HighlightOpportunity(iconRect, "SidearmMissing");

            if (Widgets.ButtonInvisible(iconRect, true))
            {
                interactedWith = SidearmsListInteraction.WeaponMemory;
                interactionWeaponType = weaponType;
                interactionWeaponIsDuplicate = isDuplicate;
            }
        }

        public void DrawIconForWeapon(Pawn pawn, CompSidearmMemory pawnMemory, ThingWithComps weapon, List<ThingDefStuffDefPair> unsatisfiedMemories, bool isDuplicate, Rect contentRect, Vector2 iconOffset)
        {
            if (weapon is null || weapon.def is null || weapon.def.uiIcon is null)
                return;

            ThingDefStuffDefPair weaponType = weapon.toThingDefStuffDefPair();

            bool allowInteraction = StatCalculator.canUseSidearmInstance(weapon, pawn, out string interactionBlockedReason) || Settings.AllowBlockedWeaponUse;

            var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, IconSize, IconSize);
            //var iconColor = iconBaseColor;

            string hoverText;
            if (allowInteraction)
            {
                if (pawn.Drafted)
                {
                    if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                    {
                        hoverText = "DrawSidearm_gizmoTooltipOffhandWhileDrafted".Translate();
                    }
                    else if (pawnMemory.ForcedWeaponWhileDrafted == weaponType)
                    {
                        hoverText = "DrawSidearm_gizmoTooltipForcedWhileDrafted".Translate();
                    }
                    else
                    {
                        hoverText = "DrawSidearm_gizmoTooltipWhileDrafted".Translate();
                    }
                }
                else
                {
                    if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                    {
                        hoverText = "DrawSidearm_gizmoTooltipOffhand".Translate();
                    }
                    else if (pawnMemory.ForcedWeapon == weaponType)
                    {
                        hoverText = "DrawSidearm_gizmoTooltipForced".Translate();
                    }
                    else
                    {
                        if (weapon.def.IsRangedWeapon)
                        {
                            if (pawnMemory.DefaultRangedWeapon == weaponType)
                                hoverText = "DrawSidearm_gizmoTooltipRangedDefault".Translate();
                            else
                                hoverText = "DrawSidearm_gizmoTooltipRanged".Translate();
                        }
                        else
                        {
                            if (pawnMemory.PreferredMeleeWeapon == weaponType)
                                hoverText = "DrawSidearm_gizmoTooltipMeleePreferred".Translate();
                            else
                                hoverText = "DrawSidearm_gizmoTooltipMelee".Translate();
                        }
                    }
                }
            }
            else 
            {
                hoverText = "DrawSidearm_blocked".Translate() + ": " + interactionBlockedReason;
            }

            TooltipHandler.TipRegion(iconRect, string.Format(hoverText, weapon.Label));
            MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);

            bool isMemorised = unsatisfiedMemories.Contains(weaponType);

            Texture2D drawPocket;
            if (isMemorised)
            {
                unsatisfiedMemories.Remove(weaponType);
                drawPocket = TextureResources.drawPocket;
            }
            else
            {
                drawPocket = TextureResources.drawPocketTemp;
            }

            if (Mouse.IsOver(iconRect))
            {
                GUI.color = iconMouseOverColor;
                GUI.DrawTexture(iconRect, drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconMouseOverColor);
            }
            else
            {
                GUI.color = iconBaseColor;
                GUI.DrawTexture(iconRect, drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconBaseColor);
            }

            Graphic outerGraphic = weaponType.thing.graphic;
            if (outerGraphic is Graphic_StackCount)
                outerGraphic = (outerGraphic as Graphic_StackCount).SubGraphicForStackCount(weapon.stackCount, weaponType.thing);

            Material material = outerGraphic.ExtractInnerGraphicFor(null).MatAt(weaponType.thing.defaultPlacingRot, null);
            Texture resolvedIcon = (Texture2D)material.mainTexture;
            GUI.color = weapon.DrawColor;
            GUI.DrawTexture(iconRect, resolvedIcon);
            GUI.color = Color.white;

            //weapon type icons
            {
                if (GettersFilters.isManualUse(weapon))
                {
                    GUI.DrawTexture(iconRect, TextureResources.weaponTypeManual);
                }
                if (GettersFilters.isEMPWeapon(weapon))
                {
                    GUI.DrawTexture(iconRect, TextureResources.weaponTypeEMP);
                }
                if (GettersFilters.isDangerousWeapon(weapon))
                {
                    GUI.DrawTexture(iconRect, TextureResources.weaponTypeDangerous);
                }
            }


            GUI.color = Color.white;

            if (!allowInteraction) 
            {
                GUI.DrawTexture(iconRect, TextureResources.blockedWeapon);
            }

            if (weapon.stackCount > 1)
            {
                var store = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(iconRect, weapon.stackCount.ToString());
                Text.Anchor = store;
            }

            //if ( || ((pawn.equipment.Primary == null || pawn.equipment.Primary.toThingDefStuffDefPair() != weaponType) && !isDuplicate))
            {
                if (pawnMemory.ForcedWeaponWhileDrafted == weaponType && pawn.equipment.Primary == weapon)
                    GUI.DrawTexture(iconRect, TextureResources.forcedDrafted);

                if (pawnMemory.ForcedWeapon == weaponType && pawn.equipment.Primary == weapon)
                    GUI.DrawTexture(iconRect, TextureResources.forcedAlways);

                if (weaponType.thing.IsRangedWeapon & pawnMemory.DefaultRangedWeapon == weaponType)
                    GUI.DrawTexture(iconRect, TextureResources.defaultRanged);
                else if (pawnMemory.PreferredMeleeWeapon == weaponType)
                    GUI.DrawTexture(iconRect, TextureResources.preferredMelee);
            }


            Rect offhandRect = iconRect.TopPartPixels(14f).RightPartPixels(14f);

            //shield/offhand icons
            if (VFECore.active || Tacticowl.active)
            {
                if (pawn.equipment.Primary == weapon || (Tacticowl.active && Tacticowl.isOffHand(weapon)))
                {
                    //already equipped
                }
                else
                {
                    if (VFECore.active)
                    {
                        if (Mouse.IsOver(iconRect))
                        {
                            if (!VFECore.usableWithShields(weaponType.thing))
                            {
                                var shield = VFECore.offHandShield(pawn);
                                if (shield != null)
                                {
                                    //two-handed and would unequip current off-hand weapon
                                    GUI.color = Color.red;
                                    GUI.DrawTexture(offhandRect, TextureResources.weaponTypeShieldCompat);
                                    TooltipHandler.TipRegion(iconRect, "SS_EquipWarning_CannotUseWithShield".Translate(weapon.Label, shield.Label));
                                }
                            }
                            /*else
                            {
                                GUI.color = Color.white;
                                GUI.DrawTexture(offhandRect, TextureResources.weaponTypeShieldCompat);
                            }*/
                        }
                    }
                    if (Tacticowl.active) 
                    {
                        bool iconsDrawn = false;
                        if (Mouse.IsOver(iconRect))
                        {
                            if (Tacticowl.isTwoHanded(weaponType.thing))
                            {
                                if(Tacticowl.getOffHand(pawn, out ThingWithComps offhandWeapon))
                                {
                                    //two-handed and would unequip current off-hand weapon
                                    GUI.color = Color.red;
                                    GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                    TooltipHandler.TipRegion(iconRect, "SS_EquipWarning_WillUnequipOffhand".Translate(weapon.Label, offhandWeapon.Label));
                                    iconsDrawn = true;
                                }
                                else
                                {
                                    //two handed, but no offhand weapon to worry about - draw no icon
                                }
                            }
                            else if(Mouse.IsOver(offhandRect) && Tacticowl.canBeOffHand(weaponType.thing))
                            {
                                if(pawn.equipment.Primary == null)
                                {
                                    //cannot equip as offhand with no primary
                                    GUI.color = Color.gray;
                                    GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                    TooltipHandler.TipRegion(offhandRect, "SS_EquipOffandFail_NoPrimary".Translate(weapon.Label));
                                    iconsDrawn = true;
                                }
                                else 
                                { 
                                    if(Tacticowl.isTwoHanded(pawn.equipment.Primary.def))
                                    {
                                        //cannot equip as offhand if primary is a two-hander
                                        GUI.color = Color.gray;
                                        GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                        TooltipHandler.TipRegion(offhandRect, "SS_EquipOffandFail_PrimaryTwoHanded".Translate(weapon.Label, pawn.equipment.Primary.Label));
                                        iconsDrawn = true;
                                    }
                                    else if(VFECore.active && VFECore.offHandShield(pawn) != null)
                                    {
                                        var shield = VFECore.offHandShield(pawn);
                                        GUI.color = Color.gray;
                                        GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                        TooltipHandler.TipRegion(offhandRect, "SS_EquipOffandFail_AlreadyHoldingShield".Translate(weapon.Label, shield.Label));
                                        iconsDrawn = true;
                                    }
                                    else{
                                        GUI.color = Color.green;
                                        GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                        TooltipHandler.TipRegion(offhandRect, "SS_EquipAsOffand".Translate(weapon.Label));
                                        iconsDrawn = true;
                                    }
                                }
                            }
                            else 
                            {
                                //nothing special
                            }
                        }
                        if(!iconsDrawn)
                        {
                            if(Tacticowl.canBeOffHand(weaponType.thing))
                            {
                                GUI.color = Color.white;
                                GUI.DrawTexture(offhandRect, TextureResources.weaponTypeOffhandCompat);
                                TooltipHandler.TipRegion(offhandRect, "SS_CanEquipAsOffand".Translate(weapon.Label));
                            }
                        }
                    }
                }
            }

            //equip status icons
            if (pawn.equipment.Primary == weapon)
            {
                if (Tacticowl.active && !Tacticowl.isTwoHanded(weaponType.thing))
                    GUI.DrawTexture(iconRect, TextureResources.weaponHeldMainhand);
                else
                    GUI.DrawTexture(iconRect, TextureResources.weaponHeldTwohand);
            }
            else
            {
                if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                {
                    GUI.DrawTexture(iconRect, TextureResources.weaponHeldOffhand);
                }
            }
            

            if (allowInteraction)
            {
                UIHighlighter.HighlightOpportunity(iconRect, "SidearmInInventory");
                if (weapon.def.IsRangedWeapon)
                    UIHighlighter.HighlightOpportunity(iconRect, "SidearmInInventoryRanged");
                else
                    UIHighlighter.HighlightOpportunity(iconRect, "SidearmInInventoryMelee");

                if(Tacticowl.active && Tacticowl.dualWieldActive() && pawn.equipment.Primary != weapon && !Tacticowl.isOffHand(weapon) && Tacticowl.canBeOffHand(weaponType.thing) && pawn.equipment.Primary != null && !Tacticowl.isTwoHanded(pawn.equipment.Primary.def))
                {
                    if(!VFECore.active || VFECore.offHandShield(pawn) == null)
                    {
                        UIHighlighter.HighlightOpportunity(offhandRect, "SidearmOffhandable");

                        if (Widgets.ButtonInvisible(offhandRect, true))
                        {
                            if (isMemorised)
                                interactedWith = SidearmsListInteraction.Weapon;
                            else
                                interactedWith = SidearmsListInteraction.UnmemorisedWeapon;
                            interactionAsOffhand = true;
                            interactionWeapon = weapon;
                            interactionWeaponIsDuplicate = isDuplicate;
                            return;
                        }
                    }
                }
                if (Widgets.ButtonInvisible(iconRect, true))
                {
                    if (isMemorised)
                        interactedWith = SidearmsListInteraction.Weapon;
                    else
                        interactedWith = SidearmsListInteraction.UnmemorisedWeapon;
                    interactionAsOffhand = false;
                    interactionWeapon = weapon;
                    interactionWeaponIsDuplicate = isDuplicate;
                    return;
                }
            }
        }

        public void DrawIconForUnarmed(Pawn pawn, CompSidearmMemory pawnMemory, Rect contentRect, Vector2 iconOffset)
        {
            var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, IconSize, IconSize);
            //var iconColor = iconBaseColor;

            string hoverText;
            if (pawn.Drafted)
            {
                if (pawnMemory.ForcedUnarmedWhileDrafted)
                    hoverText = "DrawSidearm_gizmoTooltipUnarmedForcedWhileDrafted";
                else
                    hoverText = "DrawSidearm_gizmoTooltipUnarmedWhileDrafted";
            }
            else
            {
                if (pawnMemory.ForcedUnarmed)
                    hoverText = "DrawSidearm_gizmoTooltipUnarmedForced";
                else if (pawnMemory.PreferredUnarmed)
                    hoverText = "DrawSidearm_gizmoTooltipUnarmedPreferred";
                else
                    hoverText = "DrawSidearm_gizmoTooltipUnarmed";

            }

            TooltipHandler.TipRegion(iconRect, hoverText.Translate());
            MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);

            Texture2D drawPocket = TextureResources.drawPocket;

            if (Mouse.IsOver(iconRect))
            {
                GUI.color = iconMouseOverColor;
                GUI.DrawTexture(iconRect, drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconMouseOverColor);
            }
            else
            {
                GUI.color = iconBaseColor;
                GUI.DrawTexture(iconRect, drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconBaseColor);
            }

            Texture resolvedIcon = TexCommand.AttackMelee;
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, resolvedIcon);
            GUI.color = Color.white;

            GUI.color = Color.white; 
            
            if (pawnMemory.ForcedUnarmedWhileDrafted)
                GUI.DrawTexture(iconRect, TextureResources.forcedDrafted);
            
            if (pawnMemory.ForcedUnarmed)
                GUI.DrawTexture(iconRect, TextureResources.forcedAlways);
            
            if (pawnMemory.PreferredUnarmed)
                GUI.DrawTexture(iconRect, TextureResources.preferredMelee);
            else 
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(iconRect, true))
            {
                interactedWith = SidearmsListInteraction.Unarmed;
            }
        }

        public override void ProcessInput(Event ev)
        {
            if (activateSound != null)
            {
                activateSound.PlayOneShotOnCamera();
            }
            if (ev.button < 0)
            {
                if (hotkeyAction != null)
                    hotkeyAction();
            }
            else {
                handleInteraction(interactedWith, ev);
                interactedWith = SidearmsListInteraction.None;
                //iconClickAction(ev.button);
            }

        }

        //Ive rewritten this twice now and its still an ugly monster.
        public const int LEFT_CLICK = 0;
        public const int RIGHT_CLICK = 1;
        public void handleInteraction(SidearmsListInteraction interaction, Event ev)
        {
            if (pawnMemory == null)
                return;

            var dropMode = parent.Drafted ? DroppingModeEnum.Combat : DroppingModeEnum.Calm;


            if (ev.button == LEFT_CLICK)
            {
                switch (interaction)
                {
                    case SidearmsListInteraction.SelectorRanged:
                        pawnMemory.primaryWeaponMode = PrimaryWeaponMode.Ranged;
                        break;
                    case SidearmsListInteraction.SelectorSkill:
                        pawnMemory.primaryWeaponMode = PrimaryWeaponMode.BySkill;
                        break;
                    case SidearmsListInteraction.SelectorMelee:
                        pawnMemory.primaryWeaponMode = PrimaryWeaponMode.Melee;
                        break;
                    case SidearmsListInteraction.Weapon:
                        {
                            ThingWithComps weapon = interactionWeapon;
                            ThingDefStuffDefPair weaponType = weapon.toThingDefStuffDefPair();
                            if (parent.Drafted)
                            {
                                if (interactionAsOffhand)
                                {
                                    WeaponAssignment.EquipSpecificWeaponFromInventoryAsOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                }
                                else
                                {
                                    pawnMemory.SetWeaponAsForced(weaponType, true);
                                    if (parent.equipment.Primary != weapon && weapon is ThingWithComps)
                                    {
                                        WeaponAssignment.EquipSpecificWeaponFromInventory(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                    }
                                }
                            }
                            else
                            {
                                if (interactionAsOffhand)
                                {
                                    WeaponAssignment.EquipSpecificWeaponFromInventoryAsOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                }
                                else 
                                {
                                    if (pawnMemory.DefaultRangedWeapon == weaponType || pawnMemory.PreferredMeleeWeapon == weaponType || weaponType.isToolNotWeapon())
                                    {
                                        pawnMemory.SetWeaponAsForced(weaponType, false);
                                        if (parent.equipment.Primary != weapon && weapon is ThingWithComps)
                                        {
                                            WeaponAssignment.EquipSpecificWeaponFromInventory(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                        }
                                    }
                                    else
                                    {
                                        if (weaponType.thing.IsRangedWeapon)
                                        {
                                            pawnMemory.SetRangedWeaponTypeAsDefault(weaponType);
                                        }
                                        else
                                        {
                                            pawnMemory.SetMeleeWeaponTypeAsPreferred(weaponType);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SidearmsListInteraction.UnmemorisedWeapon:
                        {
                            ThingWithComps weapon = interactionWeapon;
                            ThingDefStuffDefPair weaponType = weapon.toThingDefStuffDefPair();
                            if (parent.Drafted)
                            {
                                if(interactionAsOffhand)
                                {
                                    WeaponAssignment.EquipSpecificWeaponFromInventoryAsOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                }
                                else
                                {
                                    pawnMemory.SetWeaponAsForced(weaponType, true);
                                    if (parent.equipment.Primary != weapon && weapon is ThingWithComps)
                                    {
                                        WeaponAssignment.EquipSpecificWeaponFromInventory(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                    }
                                }

                            }
                            else
                            {
                                pawnMemory.InformOfAddedSidearm(weapon);
                            }
                        }
                        break;
                    case SidearmsListInteraction.WeaponMemory:
                        ThingDefStuffDefPair weaponMemory = interactionWeaponType.Value;
                        if (parent.Drafted)
                        {
                            //allow nothing
                        }
                        else
                        {
                            if (weaponMemory.thing.IsRangedWeapon)
                            {
                                pawnMemory.SetRangedWeaponTypeAsDefault(weaponMemory);
                            }
                            else
                            {
                                pawnMemory.SetMeleeWeaponTypeAsPreferred(weaponMemory);
                            }
                        }
                        break;
                    case SidearmsListInteraction.Unarmed:
                        if (parent.Drafted)
                        {
                            pawnMemory.SetUnarmedAsForced(true);
                            if (parent.equipment.Primary != null)
                                WeaponAssignment.EquipSpecificWeapon(parent, null, MiscUtils.shouldDrop(parent, dropMode, false), false);
                        }
                        else if (pawnMemory.PreferredUnarmed)
                        {
                            pawnMemory.SetUnarmedAsForced(false);
                            if (parent.equipment.Primary != null)
                                WeaponAssignment.EquipSpecificWeapon(parent, null, MiscUtils.shouldDrop(parent, dropMode, false), false);
                        }
                        else 
                        {
                            pawnMemory.SetUnarmedAsPreferredMelee();
                        }
                        break;
                    case SidearmsListInteraction.None:
                    default:
                        return;
                }
            }
            else if(ev.button == RIGHT_CLICK)
            {
                switch (interaction)
                {
                    case SidearmsListInteraction.SelectorRanged:
                    case SidearmsListInteraction.SelectorSkill:
                    case SidearmsListInteraction.SelectorMelee:
                        break;
                    case SidearmsListInteraction.Weapon:
                        {
                            ThingWithComps weapon = interactionWeapon;
                            ThingDefStuffDefPair weaponType = weapon.toThingDefStuffDefPair();

                            /*if (interactionWeaponIsDuplicate)
                            {
                                WeaponAssingment.dropSidearm(parent, weapon, true);
                            }
                            else*/
                            {
                                if (parent.Drafted)
                                {
                                    if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                                    {
                                        WeaponAssignment.UnequipOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                    }
                                    else if (pawnMemory.ForcedWeaponWhileDrafted == weaponType && parent.equipment.Primary == weapon)
                                    {
                                        pawnMemory.UnsetForcedWeapon(true);
                                    }
                                    /*else
                                    {
                                        WeaponAssingment.DropSidearm(parent, weapon, true, false);
                                    }*/
                                }
                                else
                                {
                                    if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                                    {
                                        WeaponAssignment.UnequipOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                    }
                                    else if (pawnMemory.ForcedWeapon == weaponType && parent.equipment.Primary == weapon)
                                    {
                                        pawnMemory.UnsetForcedWeapon(false);
                                    }
                                    else if (weaponType.thing.IsRangedWeapon & pawnMemory.DefaultRangedWeapon == weaponType)
                                    {
                                        pawnMemory.UnsetRangedWeaponDefault();
                                    }
                                    else if (pawnMemory.PreferredMeleeWeapon == weaponType)
                                    {
                                        pawnMemory.UnsetMeleeWeaponPreference();
                                    }
                                    else
                                    {
                                        WeaponAssignment.DropSidearm(parent, weapon, true, true);
                                    }
                                }
                            }
                        }
                        break;
                    case SidearmsListInteraction.UnmemorisedWeapon:
                        {
                            ThingWithComps weapon = interactionWeapon;
                            ThingDefStuffDefPair weaponType = weapon.toThingDefStuffDefPair();

                            if (parent.Drafted)
                            {
                                if (Tacticowl.active && Tacticowl.isOffHand(weapon))
                                {
                                    WeaponAssignment.UnequipOffhand(parent, weapon, MiscUtils.shouldDrop(parent, dropMode, false), false);
                                }
                                else if (pawnMemory.ForcedWeaponWhileDrafted == weaponType && parent.equipment.Primary == weapon)
                                {
                                    pawnMemory.UnsetForcedWeapon(true);
                                }
                                else
                                {
                                    WeaponAssignment.DropSidearm(parent, weapon, true, false);
                                }
                            }
                            else{
                                WeaponAssignment.DropSidearm(parent, weapon, true, false);
                            }
                        }
                        break;
                    case SidearmsListInteraction.WeaponMemory:
                        ThingDefStuffDefPair weaponMemory = interactionWeaponType.Value;

                        /*if (interactionWeaponIsDuplicate)
                        {
                            pawnMemory.ForgetSidearmMemory(weaponMemory);
                        }
                        else*/
                        {
                            if (parent.Drafted)
                            {
                                if (pawnMemory.ForcedWeaponWhileDrafted == weaponMemory)
                                {
                                    pawnMemory.UnsetForcedWeapon(true);
                                }
                            }
                            else
                            {
                                if (pawnMemory.ForcedWeapon == weaponMemory)
                                {
                                    pawnMemory.UnsetForcedWeapon(false);
                                }
                                else if (weaponMemory.thing.IsRangedWeapon & pawnMemory.DefaultRangedWeapon == weaponMemory)
                                {
                                    pawnMemory.UnsetRangedWeaponDefault();
                                }
                                else if (pawnMemory.PreferredMeleeWeapon == weaponMemory)
                                {
                                    pawnMemory.UnsetMeleeWeaponPreference();
                                }
                                else
                                {
                                    pawnMemory.ForgetSidearmMemory(weaponMemory);
                                }
                            }
                        }

                        break;
                    case SidearmsListInteraction.Unarmed:
                        if (parent.Drafted && pawnMemory.ForcedUnarmedWhileDrafted)
                        {
                            pawnMemory.UnsetUnarmedAsForced(true);
                        }
                        else if (pawnMemory.ForcedUnarmed)
                        {
                            pawnMemory.UnsetUnarmedAsForced(false);
                        }
                        else if (pawnMemory.PreferredUnarmed)
                        {
                            pawnMemory.UnsetMeleeWeaponPreference();
                        }
                        break;
                    case SidearmsListInteraction.None:
                    default:
                        return;
                }
            }
        }


        public void DrawGizmoLabel(string labelText, Rect gizmoRect)
        {
            var labelHeight = Text.CalcHeight(labelText, gizmoRect.width);
            labelHeight -= 2f;
            var labelRect = new Rect(gizmoRect.x, gizmoRect.yMax - labelHeight + 12f, gizmoRect.width, labelHeight);
            GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }


        public int countMissingMeleeWeapons(CompSidearmMemory pawnMemory, Pawn pawn)
        {
            if (pawnMemory == null)
                return 0;

            int count = 0;

            Dictionary<ThingDefStuffDefPair, int> dupeCounters = new Dictionary<ThingDefStuffDefPair, int>();

            foreach (var group in meleeWeaponMemories.GroupBy(m => m))
            {
                var weapon = group.Key;
                var stackCount = group.Count();

                if (!dupeCounters.ContainsKey(weapon))
                    dupeCounters[weapon] = 0;

                var missingWeapons = pawn.missingCountWeaponsOfType(weapon, stackCount, dupeCounters[weapon]);
                if (missingWeapons > 0)
                    count++;

                dupeCounters[weapon] += stackCount;
            }

            return count;
        }

        public int countMissingRangedWeapons(CompSidearmMemory pawnMemory, Pawn pawn)
        {
            if (pawnMemory == null)
                return 0;

            int count = 0;

            Dictionary<ThingDefStuffDefPair, int> dupeCounters = new Dictionary<ThingDefStuffDefPair, int>();

            foreach (var group in rangedWeaponMemories.GroupBy(m => m))
            {
                var weapon = group.Key;
                var stackCount = group.Count();

                if (!dupeCounters.ContainsKey(weapon))
                    dupeCounters[weapon] = 0;

                var missingWeapons = pawn.missingCountWeaponsOfType(weapon, stackCount, dupeCounters[weapon]);
                if (missingWeapons > 0)
                    count++;

                dupeCounters[weapon] += stackCount;
            }

            return count;
        }

    }
}
