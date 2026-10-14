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
        CheckGroupsAndEfficiency();
        CheckTargetEligibility();
        CheckEffectDisplay();
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
        var armDef = new BodyPartDef { defName = "CatalogArm", label = "arm", hitPoints = 30 };
        var left = new BodyPartRecord { def = armDef, customLabel = "left arm" };
        var right = new BodyPartRecord { def = armDef, customLabel = "right arm" };
        var body = new BodyDef { defName = "TestBody", corePart = left };
        body.AllParts.AddRange(new[] { left, right });
        var race = new ThingDef { defName = "TestRace", category = ThingCategory.Pawn,
            race = new RaceProperties { body = body, intelligence = Intelligence.Humanlike,
                lifeStageAges = new List<LifeStageAge> { new LifeStageAge { def = new LifeStageDef { healthScaleFactor = 1f } } } },
            recipes = new List<RecipeDef>() };
        var pawn = new Pawn { def = race };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        AccessTools.Field(typeof(Pawn_AgeTracker), "cachedLifeStageIndex").SetValue(pawn.ageTracker, 0);
        // 110%는 자연 부위보다 높지만 현재 왼팔(125%)보다 낮습니다.
        var low = Part("CatalogLow", 1.1f);
        var high = Part("CatalogHigh", 1.25f);
        var top = Part("CatalogTop", 1.5f);
        var vanilla = FakeMod("ludeon.rimworld", true);
        foreach (var implant in new[] { low, high, top }) implant.modContentPack = vanilla;
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

        // 실제 게임의 부위 효율 함수를 사용하는 자연 부위/손상/결손 검증입니다.
        var weak = Part("NaturalDowngrade", 0.7f);
        weak.modContentPack = vanilla;
        race.recipes.Add(Recipe("InstallWeak", weak));
        // AllRecipes는 게임 내부 캐시이므로 새 수술은 기존 목록에도 반영합니다.
        race.AllRecipes.Add(race.recipes.Last());
        Check(ImplantCatalog.Build(pawn).Rows.Where(x => x.Implant == weak).All(x => x.Inferior),
            "healthy natural part hides 70 percent replacements");
        var wound = new HediffDef { defName = "TestWound", hediffClass = typeof(Hediff_Injury), initialSeverity = 15f };
        var injury = new Hediff_Injury { def = wound, pawn = pawn, Part = left };
        // Severity 프로퍼티의 월드 알림을 호출하지 않는 간이 테스트 데이터입니다.
        AccessTools.Field(typeof(Hediff), "severityInt").SetValue(injury, 15f);
        pawn.health.hediffSet.hediffs.Add(injury);
        Check(!ImplantCatalog.Build(pawn).Rows.Single(x => x.Implant == weak && x.Part == left).Inferior,
            "injured natural part uses actual reduced efficiency");
        pawn.health.hediffSet.hediffs.Clear();
        pawn.health.hediffSet.hediffs.Add(new Hediff_MissingPart { def = new HediffDef { defName = "Missing" }, pawn = pawn, Part = left });
        Check(!ImplantCatalog.Build(pawn).Rows.Single(x => x.Implant == weak && x.Part == left).Inferior,
            "missing part keeps replacement candidates");

        var blocked = Recipe("BlockedTarget", Part("BlockedImplant", 2f));
        blocked.workerClass = typeof(BlockedInstaller);
        race.AllRecipes.Add(blocked);
        var targetFiltered = ImplantCatalog.Build(pawn);
        Check(targetFiltered.Rows.All(x => x.Implant != blocked.addsHediff), "worker target restriction removes candidates");
        Check(targetFiltered.FailedRecipes == 0, "target rejection is not a failed recipe");
    }

    private static ModContentPack FakeMod(string id, bool official = false)
    {
        // 실제 생성자는 콘텐츠를 마운트합니다. 테스트에서는 출처 메타데이터만 만듭니다.
        var mod = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        AccessTools.Field(typeof(ModContentPack), "packageIdInt").SetValue(mod, id);
        AccessTools.Field(typeof(ModContentPack), "nameInt").SetValue(mod, id);
        AccessTools.Field(typeof(ModContentPack), "official").SetValue(mod, official);
        return mod;
    }

    private static void CheckGroupsAndEfficiency()
    {
        var vanilla = FakeMod("ludeon.rimworld", true);
        var fireMod = FakeMod("test.fire");
        var otherMod = FakeMod("test.other");
        HediffDef Make(string name, float efficiency, ModContentPack source)
        {
            var def = Part(name, efficiency);
            def.modContentPack = source;
            return def;
        }
        var wood = Make("Wood", .5f, vanilla);
        var prosthetic = Make("Prosthetic", .8f, vanilla);
        var bionic = Make("Bionic", 1.25f, vanilla);
        var archotech = Make("Archotech", 1.5f, vanilla);
        var fire = Make("Fire", 1.05f, fireMod);
        var lava = Make("Lava", 1.1f, fireMod);
        // 같은 계열 수치 비교는 특수 컴포넌트가 있어도 사용자가 요청한 효율 기준을 적용합니다.
        fire.comps = new List<HediffCompProperties> { new HediffCompProperties() };
        var part = new BodyPartRecord { def = new BodyPartDef { defName = "Arm" } };
        bool Hide(HediffDef candidate, HediffDef installed) => ImplantComparison.ShouldHide(candidate,
            installed, part, installed.addedPartProps.partEfficiency, false);
        Check(Hide(wood, bionic) && Hide(prosthetic, bionic), "bionic hides wooden and prosthetic arms");
        Check(!Hide(archotech, bionic) && !Hide(fire, bionic) && !Hide(lava, bionic),
            "bionic keeps archotech and both external fire arms");
        Check(Hide(fire, lava) && !Hide(bionic, lava) && !Hide(archotech, lava),
            "lava 110 hides fire 105 and keeps vanilla upgrades");
        Check(!Hide(lava, fire) && !Hide(bionic, fire) && !Hide(archotech, fire),
            "fire 105 keeps lava and both vanilla upgrades");
        Check(Hide(wood, lava) && Hide(prosthetic, lava), "mod arm hides inferior vanilla replacements");
        Check(!Hide(Make("OtherSpecial", .9f, otherMod), lava), "different mod group stays separate");
        Check(!Hide(Make("Equal", 1.1f, fireMod), lava), "equal efficiency stays visible");
        Check(!ImplantComparison.ShouldHide(lava, null, part, float.NaN, false), "invalid current efficiency fails open");
        Check(!ImplantComparison.ShouldHide(Make("Invalid", float.NaN, fireMod), lava, part, 1.1f, false),
            "invalid candidate efficiency fails open");
        fire.modExtensions = new List<DefModExtension> { new ImplantGroupExtension { comparisonGroup = "fire.utility", groupLabel = "Utility" } };
        Check(!Hide(fire, lava), "explicit subgroup preserves specialized lower-efficiency arm");
        lava.modExtensions = new List<DefModExtension> { new ImplantGroupExtension { comparisonGroup = "fire.utility" } };
        Check(Hide(fire, lava), "same explicit subgroup compares again");
        Check(ImplantGroups.Key(Make("DLCPart", 1f, FakeMod("ludeon.rimworld.biotech", true))) == ImplantGroups.Vanilla,
            "official expansions share vanilla group");
        Check(ImplantGroups.Key(Part("UnknownA", 1f)) != ImplantGroups.Key(Part("UnknownB", 1f)),
            "unknown sources are not assumed to be one family");
    }

    private static void CheckTargetEligibility()
    {
        var pawn = new Pawn { def = new ThingDef { race = new RaceProperties { intelligence = Intelligence.Humanlike } } };
        pawn.health = new Pawn_HealthTracker(pawn);
        var implant = Part("GhoulPart", 1f);
        var ghoul = new MutantDef { defName = "TestGhoul" };
        var recipe = new RecipeDef { mutantPrerequisite = new List<MutantDef> { ghoul } };
        Check(!TargetEligibility.Allows(pawn, recipe, implant), "human cannot see ghoul-only implant");
        pawn.mutant = new Pawn_MutantTracker(pawn);
        AccessTools.Field(typeof(Pawn_MutantTracker), "def").SetValue(pawn.mutant, ghoul);
        Check(TargetEligibility.Allows(pawn, recipe, implant), "ghoul can see ghoul-only implant");
        AccessTools.Field(typeof(Pawn_MutantTracker), "def").SetValue(pawn.mutant, new MutantDef { defName = "OtherMutant" });
        Check(!TargetEligibility.Allows(pawn, recipe, implant), "other mutant is not treated as ghoul");
        AccessTools.Field(typeof(Pawn_MutantTracker), "def").SetValue(pawn.mutant, ghoul);
        recipe.mutantPrerequisite = null;
        recipe.mutantBlacklist = new List<MutantDef> { ghoul };
        Check(!TargetEligibility.Allows(pawn, recipe, implant), "mutant blacklist respected");
        pawn.mutant = null;
        var mechlink = new HediffDef { defName = "MechlinkImplant" };
        var control = new HediffDef { defName = "TestControl" };
        DefDatabase<ThingDef>.Add(new ThingDef { defName = "ControlItem", comps = new List<CompProperties>
        {
            new CompProperties_Usable { userMustHaveHediff = mechlink },
            new CompProperties_UseEffectInstallImplant { hediffDef = control }
        } });
        TargetEligibility.Invalidate();
        recipe = new RecipeDef();
        Check(!TargetEligibility.Allows(pawn, recipe, control), "non-mechanitor cannot see control implant");
        Check(TargetEligibility.Allows(pawn, recipe, mechlink), "mechlink itself remains available to non-mechanitor");
        pawn.health.hediffSet.hediffs.Add(new Hediff { def = mechlink, pawn = pawn });
        Check(TargetEligibility.Allows(pawn, recipe, control), "mechanitor can see control implant");
        pawn.health.hediffSet.hediffs.Clear();
        recipe.modExtensions = new List<DefModExtension> { new ImplantRecipeExtension { requiresMechanitor = true } };
        Check(!TargetEligibility.Allows(pawn, recipe, implant), "mod extension can require mechanitor");
        pawn.health.hediffSet.hediffs.Add(new Hediff { def = mechlink, pawn = pawn });
        Check(TargetEligibility.Allows(pawn, recipe, implant), "mechanitor satisfies explicit target restriction");
    }

    private static void CheckEffectDisplay()
    {
        // 화면/설정 폴더 없이 번역의 {0} 치환만 검증할 수 있게 최소 언어 데이터를 구성합니다.
        var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "English";
        language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement>();
        var translations = System.Xml.Linq.XDocument.Load(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../Languages/English/Keyed/CanMoreImplant.xml")));
        foreach (var item in translations.Root.Elements())
            language.keyedReplacements[item.Name.LocalName] = new LoadedLanguage.KeyedReplacement { key = item.Name.LocalName, value = item.Value };
        foreach (var pair in new Dictionary<string, string> { ["PartEfficiency"] = "Part efficiency", ["Pain"] = "Pain",
            ["Stat_Hediff_Pain_Desc"] = "Pain effect", ["WholeBody"] = "Whole body" })
            language.keyedReplacements[pair.Key] = new LoadedLanguage.KeyedReplacement { key = pair.Key, value = pair.Value };
        AccessTools.Field(typeof(LoadedLanguage), "dataIsLoaded").SetValue(language, true);
        AccessTools.Field(typeof(LoadedLanguage), "workerInt").SetValue(language, new LanguageWorker_English());
        LanguageDatabase.activeLanguage = language;
        var replacement = Part("DisplayArm", 1.25f);
        var summary = ImplantEffects.Build(replacement, null);
        Check(summary.Entries.Count == 1 && summary.Summary.Contains("125%"), "replacement summary shows part efficiency");
        Check(ImplantEffects.Build(Part("NaturalEquivalent", 1f), null).Summary.Contains("100%"),
            "100 percent replacement is displayed explicitly");
        var special = new HediffDef { defName = "UnknownSpecial" };
        Check(ImplantEffects.Build(special, null).Entries.Count == 1, "unknown effects have a fallback entry");
        var firstStage = new HediffStage { minSeverity = 0f, painFactor = .5f };
        var secondStage = new HediffStage { minSeverity = 1f, painFactor = .2f };
        var staged = new HediffDef { defName = "Staged", initialSeverity = .5f,
            stages = new List<HediffStage> { firstStage, secondStage } };
        Check(ReferenceEquals(ImplantEffects.SelectStage(staged, null), firstStage), "candidate uses initial not maximum stage");
        var installed = new Hediff { def = staged };
        AccessTools.Field(typeof(Hediff), "severityInt").SetValue(installed, 1.5f);
        Check(ReferenceEquals(ImplantEffects.SelectStage(staged, installed), secondStage), "installed entry uses actual stage");
        // 통증 항목까지만 실제 게임 포맷터를 읽습니다. 이후 확장팩 설정 접근은 게임 없는 테스트에서 제외합니다.
        IEnumerable<StatDrawEntry> PainEntries(HediffStage stage, Hediff instance) =>
            HediffStatsUtility.SpecialDisplayStats(stage, instance).Take(stage.painOffset == 0f ? 1 : 2);
        Check(ImplantEffects.Build(staged, null, PainEntries).Summary.Contains("50%"), "pain factor preserves multiplicative percent");
        firstStage.painOffset = -.1f;
        summary = ImplantEffects.Build(staged, null, PainEntries);
        Check(summary.Entries.Count == 2 && summary.Details.Contains("-10%") && summary.Details.Contains("50%"),
            "multiple effects preserve offset and factor in details");
        Check(ImplantEffects.Summarize(new[] { "A", "B", "C" }).StartsWith("A\n"), "summary uses first effect and extra count line");
        firstStage.multiplyStatChangesBySeverity = true;
        Check(ImplantEffects.HasUnresolvedScaling(firstStage, null), "uninstalled severity scaling is conditional");
        Check(!ImplantEffects.HasUnresolvedScaling(firstStage, installed), "installed severity scaling can use actual instance");
        firstStage.statOffsetEffectMultiplier = new StatDef { defName = "UnknownMultiplier" };
        Check(ImplantEffects.HasUnresolvedScaling(firstStage, installed), "pawn-dependent scaling is not a fixed bonus");
        var part = new BodyPartRecord { def = new BodyPartDef { defName = "DisplayPart", label = "arm" } };
        var efficiencies = new Dictionary<BodyPartRecord, float> { [part] = .75f };
        Check(ImplantTabView.PartWithEfficiency(part, efficiencies).Contains("75%"), "part column uses current efficiency");
        efficiencies[part] = float.NaN;
        Check(ImplantTabView.PartWithEfficiency(part, efficiencies).Contains("?"), "unknown current efficiency is not shown as zero");
        Check(!ImplantTabView.PartWithEfficiency(null, efficiencies).Contains("%"), "whole body has no invented efficiency");
        ImplantTabView.ColumnWidths(580f, out float partWidth, out float nameWidth, out float effectWidth);
        Check(Math.Abs(partWidth + nameWidth + effectWidth + 28f - 580f) < .001f,
            "three columns reserve info button without overflow");
        ImplantTabView.ColumnWidths(360f, out partWidth, out nameWidth, out effectWidth);
        Check(partWidth > 8f && nameWidth > 8f && effectWidth > 8f, "narrow layout retains usable column widths");
    }
}

// 고정 부위를 돌려주는 테스트용 수술. 실제 모드에는 포함되지 않습니다.
public sealed class FakeInstaller : RecipeWorker
{
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe) => pawn.RaceProps.body.AllParts;
}

public sealed class BlockedInstaller : RecipeWorker
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null) => false;
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe) => pawn.RaceProps.body.AllParts;
}
