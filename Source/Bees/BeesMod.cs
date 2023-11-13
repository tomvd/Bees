using HarmonyLib;
using PlantGenetics;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bees;

[DefOf]
public static class InternalDefOf
{
	static InternalDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
	}
	public static ThingDef Bees_Beehive;
	public static JobDef Bees_TakeHoneyOutOfBeehive;
	public static JobDef Bees_TendToBeehive;
	public static HediffDef Bees_Sting;
	public static ThoughtDef Bees_StingMoodDebuff;
	public static ThingDef Bees_Honey;
	public static ThingDef Bees_Propolis;
	public static ResearchProjectDef Bees_PropolisExtraction;
	public static DamageDef Bees_DamageSting;
	public static SoundDef Bees_Beehive_Ambience;
	public static ThingDef Bees_MeadFermentingBarrel;
	public static JobDef Bees_FillMeadFermentingBarrel;
	public static JobDef Bees_TakeMeadOutOfFermentingBarrel;
	public static ThingDef Bees_Mead;

}

public class BeesMod : Mod
{
	public static Harmony harmonyInstance;
	public static Settings Settings;
	
	public BeesMod(ModContentPack content) : base(content)
	{
		Settings = GetSettings<Settings>();
		//Log.Message("PlantGeneticsMod:start ");
		harmonyInstance = new Harmony("Adamas.PlantGenetics");
		harmonyInstance.PatchAll();        
        
    }
	
	/// <summary>
    /// The (optional) GUI part to set your settings.
    /// </summary>
    /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
    }

    /// <summary>
    /// Override SettingsCategory to show up in the list of settings.
    /// Using .Translate() is optional, but does allow for localisation.
    /// </summary>
    /// <returns>The (translated) mod name.</returns>
    public override string SettingsCategory()
    {
        return "Bees";
    }
}