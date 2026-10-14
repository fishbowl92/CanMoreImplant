using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using CanMoreImplant;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

internal static class Program
{
    private static int passed;
    private static string gamePath;
    private static string harmonyPath;

    public static int Main(string[] args)
    {
        gamePath = args.Length > 0 ? args[0] : @"E:\SteamLibrary\steamapps\common\RimWorld";
        harmonyPath = args.Length > 1 ? args[1] : @"E:\SteamLibrary\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll";
        // 게임 DLL을 배포물에 복사하지 않고 설치 폴더에서 읽어 옵니다.
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string file = name.Name == "0Harmony" ? harmonyPath : Path.Combine(gamePath, name.Name + ".dll");
            return File.Exists(file) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(file) : null;
        };
        try { Run(); Console.WriteLine($"PASS: {passed} checks"); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        CanMoreImplantMod.Settings = new ImplantSettings { useExplicitRules = false };
        HediffDef low = Part("Low", 0.8f), high = Part("High", 1.25f);
        Check(ImplantComparison.SimpleDominates(high, low), "simple efficiency upgrade");
        Check(!ImplantComparison.SimpleDominates(low, high), "not reversed");
        Check(!ImplantComparison.SimpleDominates(high, Part("Equal", 1.25f)), "equal is not inferior");
        HediffDef special = Part("Special", 0.8f);
        special.stages = new List<HediffStage> { new HediffStage() };
        Check(!ImplantComparison.SimpleDominates(high, special), "special stages preserved");
        special = Part("Abilities", 0.8f);
        special.abilities = new List<AbilityDef> { new AbilityDef { defName = "Ability" } };
        Check(!ImplantComparison.SimpleDominates(high, special), "special abilities preserved");
        special = Part("Components", 0.8f);
        special.comps = new List<HediffCompProperties> { new HediffCompProperties() };
        Check(!ImplantComparison.SimpleDominates(high, special), "unknown components preserved");
        special = Part("Extension", 0.8f);
        special.modExtensions = new List<DefModExtension> { new ImplantRecipeExtension() };
        Check(!ImplantComparison.SimpleDominates(high, special), "unknown extensions preserved");
        special = Part("DifferentEffect", 0.8f);
        special.preventsPregnancy = true;
        Check(!ImplantComparison.SimpleDominates(high, special), "non-efficiency differences preserved");
        Check(!ImplantComparison.IsInferior(low, high, null, false), "custom surgery workers not auto-ranked");
        Check(!ImplantComparison.IsInferior(high, high, null, true), "installed identity not ranked");
        Check(!ImplantCatalog.IsImplantDefinition(new HediffDef { defName = "Disease" }), "disease excluded");
        Check(ImplantCatalog.IsImplantDefinition(high), "added part discovered");

        // 명시 규칙의 간접 관계, 부위 제한, 순환 처리를 검증합니다.
        CanMoreImplantMod.Settings.useExplicitRules = true;
        var arm = new BodyPartDef { defName = "TestArm" };
        var leg = new BodyPartDef { defName = "TestLeg" };
        var armRecord = new BodyPartRecord { def = arm };
        var legRecord = new BodyPartRecord { def = leg };
        var top = Part("Top", 1.5f);
        DefDatabase<ImplantComparisonDef>.Add(new ImplantComparisonDef { defName = "Test1", lower = low, higher = high, bodyPart = arm });
        DefDatabase<ImplantComparisonDef>.Add(new ImplantComparisonDef { defName = "Test2", lower = high, higher = top, bodyPart = arm });
        Check(ImplantComparison.IsInferior(low, top, armRecord, false), "transitive explicit hierarchy");
        Check(!ImplantComparison.IsInferior(low, top, legRecord, false), "explicit rule body part scoped");
        DefDatabase<ImplantComparisonDef>.Add(new ImplantComparisonDef { defName = "Cycle", lower = top, higher = low, bodyPart = arm });
        Check(!ImplantComparison.IsInferior(low, top, armRecord, false), "cycle keeps candidates visible");

        MethodInfo health = AccessTools.Method(typeof(HealthCardUtility), "DrawHealthSummary");
        var original = PatchProcessor.GetOriginalInstructions(health);
        var modified = AddImplantTabPatch.Transpiler(original).ToList();
        Check(modified.Count(x => x.operand is MethodInfo m && m.DeclaringType == typeof(AddImplantTabPatch)) == 1,
            "installed game health tab has exactly one matching hook");
        Check(AccessTools.Field(typeof(HealthCardUtility), "onOperationTab")?.FieldType == typeof(bool), "vanilla operation state field exists");
        CheckCatalog();
        Console.WriteLine("No game UI was launched. These are logic and installed-API checks only.");
    }

    private static HediffDef Part(string name, float efficiency) => new HediffDef
    {
        defName = name, label = name, hediffClass = typeof(Hediff_AddedPart),
        addedPartProps = new AddedBodyPartProps { partEfficiency = efficiency }
    };
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        passed++;
        Console.WriteLine("OK: " + message);
    }

    private static void CheckCatalog()
    {
        CanMoreImplantMod.Settings.useExplicitRules = false;
        var armDef = new BodyPartDef { defName = "CatalogArm", label = "arm" };
        var left = new BodyPartRecord { def = armDef, customLabel = "left arm" };
        var right = new BodyPartRecord { def = armDef, customLabel = "right arm" };
        var body = new BodyDef { defName = "TestBody", corePart = left };
        body.AllParts.AddRange(new[] { left, right });
        var race = new ThingDef { defName = "TestRace", category = ThingCategory.Pawn,
            race = new RaceProperties { body = body, intelligence = Intelligence.Humanlike }, recipes = new List<RecipeDef>() };
        var pawn = new Pawn { def = race };
        pawn.health = new Pawn_HealthTracker(pawn);
        var low = Part("CatalogLow", 0.7f);
        var high = Part("CatalogHigh", 1.25f);
        var top = Part("CatalogTop", 1.5f);
        pawn.health.hediffSet.hediffs.Add(new Hediff_AddedPart { def = high, pawn = pawn, Part = left });
        RecipeDef Recipe(string name, HediffDef implant) => new RecipeDef
        {
            defName = name, label = name, addsHediff = implant, targetsBodyPart = true,
            workerClass = typeof(FakeInstaller), recipeUsers = new List<ThingDef> { race },
            modExtensions = new List<DefModExtension> { new ImplantRecipeExtension { allowAutomaticComparison = true } }
        };
        race.recipes.AddRange(new[] { Recipe("InstallLow", low), Recipe("InstallHigh", high),
            Recipe("InstallTop", top), Recipe("InstallLowAlternative", low) });
        var first = ImplantCatalog.Build(pawn);
        Check(first.Rows.Count == 6, "duplicate recipes merged per part and implant");
        Check(first.Rows.Single(x => x.Part == left && x.Implant == high).Installed != null, "installed implant retained once");
        Check(first.Rows.Single(x => x.Part == left && x.Implant == low).Inferior, "lower on installed side hidden");
        Check(!first.Rows.Single(x => x.Part == right && x.Implant == low).Inferior, "opposite side is not hidden");
        Check(!first.Rows.Single(x => x.Part == left && x.Implant == top).Inferior, "upper candidate remains");
        Check(first.Rows.Single(x => x.Part == left && x.Implant == low).Recipes.Count == 2, "alternative surgery sources retained");
        Check(pawn.health.hediffSet.hediffs.Count == 1 && pawn.BillStack.Count == 0, "catalog does not mutate health or bills");
        pawn.health.hediffSet.hediffs.Clear();
        var second = ImplantCatalog.Build(pawn);
        Check(second.Rows.All(x => !x.Inferior), "no installed part means no inferior hiding");
    }
}

// 고정 부위를 돌려주는 테스트용 수술. 실제 모드에는 포함되지 않습니다.
public sealed class FakeInstaller : RecipeWorker
{
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe) => pawn.RaceProps.body.AllParts;
}
