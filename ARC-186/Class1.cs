using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Range = SemanticVersioning.Range;

namespace ARC186;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.arc186.customitem";
    public override string Name { get; init; } = "ARC-186";
    public override string Author { get; init; } = "SpyPilot";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.3");
    public override Range SptVersion { get; init; } = new("~4.0.13");
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";

    public override List<string>? Contributors { get; init; } = new();
    public override List<string>? Incompatibilities { get; init; } = new();
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.epicrangetime.aio", new Range(">=4.0.7") },
        { "com.wtt.commonlib", new Range(">=2.0.20") },
        { "com.c11.unpracticaltactical", new Range(">=1.2.0") }
    };
    public override string? Url { get; init; } = "https://github.com/SpyPilotEFT/ARC-186";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class Arc186ItemService(
    ISptLogger<Arc186ItemService> logger,
    CustomItemService customItemService,
    CustomQuestService customQuestService,
    DatabaseService databaseService,
    BotLootCacheService botLootCacheService,
    JsonUtil jsonUtil,
    ProfileHelper profileHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad, IOnUpdate
{
    private const string Arc186Tpl = "6a0b24653f221925eea85485";
    private const string BirdpipePosterTpl = "6a0b24653f221925eea854e0";
    private const string PosterParentTpl = "6759673c76e93d8eb20b2080";
    private const string IntegratedAvionicsPartOneQuestId = "6a0b24653f221925eea85486";
    private const string IntegratedAvionicsPartTwoQuestId = "6a0b24653f221925eea854b0";
    private const string MechanicTraderId = "5a7c2eca46aef81a7ca2145d";
    private const string IntegratedAvionicsQuestImage = "6a0b24653f221925eea85486.jpg";
    private const string CofdmTpl = "5c052f6886f7746b1e3db148";
    private const string AdvancedElectronicMaterialsTpl = "6389c92d52123d5dd17f8876";
    private const string WiresTpl = "5c06779c86f77426e00dd782";
    private const string PhaseControlRelayTpl = "5d1b313086f77425227d1678";
    private const string ToolsetTpl = "590c2e1186f77425357b6124";
    private const string FlukeTpl = "6cc7d60178706926401b7c8d";
    private const string BirdeyeBotType = "followerbirdeye";
    private const string BirdeyeComm3BackpackTpl = "628bc7fb408e2b2e9c0801b1";
    private const double BirdeyeArc186BackpackWeight = 1000000d;
    private const double BirdeyeBackpackSingleLootRollWeight = 100000d;
    private const double QuestAemLooseLootWeight = 100000d;
    private const double QuestAemTrainWarehouseX = 35.2363d;
    private const double QuestAemTrainWarehouseY = 13.4919d;
    private const double QuestAemTrainWarehouseZ = -846.4481d;
    private const double QuestAemTrainWarehouseRadius = 1.0d;
    private bool lateDatabasePatchesApplied;
    private bool arc186HideoutRecipeRegistered;
    private static readonly HashSet<string> LighthouseQuestAemSpawnpointIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "(35.447 12.85 -846.65)",
        "(35.447, 13.008, -846.65)",
        "(35.29, 12.979, -845.99)",
        "(35.691, 13.008, -846.065)"
    };

    public async Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var arc186 = new NewItemFromCloneDetails
        {
            ItemTplToClone = CofdmTpl,
            ParentId = "57864a66245977548f04a81f",
            NewId = Arc186Tpl,

            OverrideProperties = new TemplateItemProperties
            {
                Name = "ARC-186",
                ShortName = "ARC-186",
                Description = "Airborne SINCGARS will provide jam-resistant, secure VHF/FM/AM communications in the cockpit. This enables interoperable communications with the Army's SINCGARS systems during close air support missions.",

                Width = 2,
                Height = 2,
                Weight = 3.18,
                BackgroundColor = "violet",

                Prefab = new Prefab
                {
                    Path = "arc-186.bundle",
                    Rcid = ""
                },

                ExaminedByDefault = true,
                StackMaxSize = 1,
                CreditsPrice = 145500,
                LootExperience = 20,
                ExamineExperience = 5
            },

            FleaPriceRoubles = 145500,
            HandbookPriceRoubles = 145500,
            HandbookParentId = "5b47574386f77428ca22b2ef",

            Locales = new Dictionary<string, LocaleDetails>
            {
                {
                    "en",
                    new LocaleDetails
                    {
                        Name = "ARC-186",
                        ShortName = "ARC-186",
                        Description = "Airborne SINCGARS will provide jam-resistant, secure VHF/FM/AM communications in the cockpit. This enables interoperable communications with the Army's SINCGARS systems during close air support missions."
                    }
                }
            }
        };

        customItemService.CreateItemFromClone(arc186);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        BirdeyeArc186GeneratedBotPatch.ProfileHelper = profileHelper;
        new BirdeyeArc186GeneratedBotPatch().Enable();
        RestrictArc186FromRagfair();

        AddArc186ToHallOfFameLargeTrophies();
        AddBirdpipePosterToHideoutPosterSlots();

        EnsureQuestImageAvailable();
        AddIntegratedAvionicsQuestFromFiles();

        await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly, System.IO.Path.Join("db", "CustomBotLoadouts"));
        await wttCommon.CustomLootspawnService.CreateCustomLootSpawns(assembly, System.IO.Path.Join("db", "CustomLootspawns"));
        if (IsIntegratedAvionicsPartTwoCompleteForAnyLoadedProfile())
        {
            await RegisterArc186HideoutRecipe(assembly);
        }
        AddForcedSpawnpointsFromConfig();
        AddQuestAemLooseSpawnpointFallback();
        ForceQuestAemLighthouseLooseSpawns();
        AddBotBackpackLoadoutFromConfig();
        EnsureBirdeyeBackpackLootRolls();
        botLootCacheService.ClearCache();

        LogStartupMessage();
    }

    public Task<bool> OnUpdate(long deltaTime)
    {
        if (!arc186HideoutRecipeRegistered && IsIntegratedAvionicsPartTwoCompleteForAnyLoadedProfile())
        {
            RegisterArc186HideoutRecipe(Assembly.GetExecutingAssembly()).GetAwaiter().GetResult();
        }

        if (lateDatabasePatchesApplied)
        {
            return Task.FromResult(false);
        }

        lateDatabasePatchesApplied = true;

        AddArc186ToAllCofdmStaticLootSpawns();
        AddBotBackpackLoadoutFromConfig();
        EnsureBirdeyeBackpackLootRolls();
        AddForcedSpawnpointsFromConfig();
        AddQuestAemLooseSpawnpointFallback();
        ForceQuestAemLighthouseLooseSpawns();
        botLootCacheService.ClearCache();

        return Task.FromResult(false);
    }

    private async Task RegisterArc186HideoutRecipe(Assembly assembly)
    {
        if (arc186HideoutRecipeRegistered)
        {
            return;
        }

        await wttCommon.CustomHideoutRecipeService.CreateHideoutRecipes(assembly, System.IO.Path.Join("db", "CustomHideoutRecipes"));
        EnsureArc186HideoutRecipeRegisteredFromJson();
        arc186HideoutRecipeRegistered = IsArc186HideoutRecipeRegistered();
    }

    private string AddIntegratedAvionicsQuestFromFiles()
    {
        var modRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (modRoot is null)
        {
            return "not added, mod folder was not found";
        }

        var customQuestDirectory = System.IO.Path.Combine(modRoot, "db", "CustomQuests", MechanicTraderId);
        var questPath = System.IO.Path.Combine(customQuestDirectory, "Quests", "quests.json");
        var localePath = System.IO.Path.Combine(customQuestDirectory, "Locales", "en.json");

        if (!System.IO.File.Exists(questPath))
        {
            return $"not added, missing {questPath}";
        }

        if (!System.IO.File.Exists(localePath))
        {
            return $"not added, missing {localePath}";
        }

        var quests = jsonUtil.Deserialize<Dictionary<string, Quest>>(System.IO.File.ReadAllText(questPath));

        if (quests is null)
        {
            return $"not added, {questPath} did not deserialize";
        }

        var englishLocales = jsonUtil.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(localePath));

        if (englishLocales is null)
        {
            return $"not added, {localePath} did not deserialize";
        }

        var addedCount = 0;
        var skippedCount = 0;

        foreach (var questEntry in quests)
        {
            if (TryGetDictionaryValue(databaseService.GetQuests(), questEntry.Key, out _))
            {
                skippedCount++;
                continue;
            }

            customQuestService.CreateQuest(new NewQuestDetails
            {
                NewQuest = questEntry.Value,
                Locales = new Dictionary<string, Dictionary<string, string>>
                {
                    { "en", englishLocales }
                }
            });

            addedCount++;
        }

        return $"added {addedCount} from CustomQuests files, skipped {skippedCount}";
    }

    private void EnsureQuestImageAvailable()
    {
        var modRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (modRoot is null)
        {
            return;
        }

        var sourcePath = System.IO.Path.Combine(modRoot, "images", "thumb_2.jpg");

        if (!System.IO.File.Exists(sourcePath))
        {
            logger.Warning("ARC-186: Quest image images/thumb_2.jpg was not found in the mod folder.");
            return;
        }

        var imageDirectory = System.IO.Path.Combine(modRoot, "db", "CustomQuests", MechanicTraderId, "Images");
        var targetPath = System.IO.Path.Combine(imageDirectory, IntegratedAvionicsQuestImage);

        System.IO.Directory.CreateDirectory(imageDirectory);
        System.IO.File.Copy(sourcePath, targetPath, true);

        var userDirectory = System.IO.Directory.GetParent(modRoot)?.Parent?.FullName;

        if (userDirectory is null)
        {
            return;
        }

        var servedIconDirectory = System.IO.Path.Combine(userDirectory, "sptappdata", "files", "quest", "icon");
        var servedIconPath = System.IO.Path.Combine(servedIconDirectory, IntegratedAvionicsQuestImage);

        System.IO.Directory.CreateDirectory(servedIconDirectory);
        System.IO.File.Copy(sourcePath, servedIconPath, true);
    }

    private void RestrictArc186FromRagfair()
    {
        foreach (var item in EnumerateDatabaseValues(databaseService.GetItems()))
        {
            var itemId = GetStringMember(item, "_id", "Id");

            if (itemId != Arc186Tpl)
            {
                continue;
            }

            var itemProperties = GetMemberValue(item, "_props", "Props", "Properties");

            if (itemProperties is null)
            {
                return;
            }

            if (!SetBooleanMember(itemProperties, false, "CanSellOnRagfair", "canSellOnRagfair"))
            {
                logger.Warning("ARC-186: Could not disable selling ARC-186 on the flea market.");
            }

            if (!SetBooleanMember(itemProperties, false, "CanRequireOnRagfair", "canRequireOnRagfair"))
            {
                logger.Warning("ARC-186: Could not disable requiring ARC-186 on the flea market.");
            }

            return;
        }
    }

    private void AddArc186ToHallOfFameLargeTrophies()
    {
        var hallOfFameContainerIds = new HashSet<string>
        {
            "63dbd45917fff4dee40fe16e",
            "65424185a57eea37ed6562e9",
            "6542435ea57eea37ed6562f5"
        };

        foreach (var item in EnumerateDatabaseValues(databaseService.GetItems()))
        {
            var itemId = GetStringMember(item, "_id", "Id");

            if (itemId is null || !hallOfFameContainerIds.Contains(itemId))
            {
                continue;
            }

            var itemProperties = GetMemberValue(item, "_props", "Props", "Properties");
            var slots = GetMemberValue(itemProperties, "Slots") as IEnumerable;

            if (slots is null)
            {
                continue;
            }

            foreach (var slot in slots)
            {
                var slotName = GetStringMember(slot, "_name", "Name");

                if (slotName is null || !slotName.StartsWith("bigTrophies", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddTplToSlotFilters(slot, Arc186Tpl);
            }
        }
    }

    private void AddBirdpipePosterToHideoutPosterSlots()
    {
        var posterTpls = EnumerateDatabaseValues(databaseService.GetItems())
            .Where(item => string.Equals(GetStringMember(item, "_parent", "Parent"), PosterParentTpl, StringComparison.OrdinalIgnoreCase))
            .Select(item => GetStringMember(item, "_id", "Id"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        posterTpls.Add(BirdpipePosterTpl);

        foreach (var item in EnumerateDatabaseValues(databaseService.GetItems()))
        {
            var itemProperties = GetMemberValue(item, "_props", "Props", "Properties");
            var slots = GetMemberValue(itemProperties, "Slots") as IEnumerable;

            if (slots is null)
            {
                continue;
            }

            foreach (var slot in slots)
            {
                AddTplToPosterSlotFilters(slot, posterTpls, BirdpipePosterTpl);
            }
        }
    }

    private int AddArc186ToAllCofdmStaticLootSpawns()
    {
        var patchedCount = 0;

        foreach (var location in EnumerateLocationObjects(databaseService.GetLocations()))
        {
            var staticLoot = GetMemberValue(location, "StaticLoot", "staticLoot");
            var staticLootValue = GetMemberValue(staticLoot, "Value") ?? staticLoot;

            if (staticLootValue is null)
            {
                continue;
            }

            foreach (var spawnPoint in EnumerateDatabaseValues(staticLootValue))
            {
                var itemDistribution = GetMemberValue(spawnPoint, "ItemDistribution", "itemDistribution");

                if (itemDistribution is not IEnumerable collection || ContainsTpl(collection, Arc186Tpl))
                {
                    continue;
                }

                var cofdmEntry = collection.Cast<object>().FirstOrDefault(entry => GetTpl(entry) == CofdmTpl);

                if (cofdmEntry is null)
                {
                    continue;
                }

                var cofdmWeight = GetNumericMember(cofdmEntry, "relativeProbability", "RelativeProbability", "weight", "Weight", "probability", "Probability");
                var arc186Weight = cofdmWeight > 0 ? cofdmWeight : 1d;

                if (AddWeightedTplToCollection(collection, cofdmEntry, Arc186Tpl, arc186Weight))
                {
                    patchedCount++;
                }
            }
        }

        return patchedCount;
    }

    private int ForceQuestAemLighthouseLooseSpawns()
    {
        var lighthouse = FindLocationObject(databaseService.GetLocations(), "lighthouse");

        if (lighthouse is null)
        {
            logger.Warning("ARC-186: Lighthouse location was not found for quest AEM loose-loot patch.");
            return 0;
        }

        var spawnpoints = GetLooseLootSpawnpoints(lighthouse) as IEnumerable;

        if (spawnpoints is null)
        {
            logger.Warning("ARC-186: Lighthouse loose-loot spawnpoints were not found for quest AEM patch.");
            return 0;
        }

        var patchedCount = 0;

        foreach (var spawnpoint in spawnpoints.Cast<object>())
        {
            var locationId = GetStringMember(spawnpoint, "locationId", "LocationId", "id", "Id");

            if (!IsQuestAemSpawnpoint(spawnpoint, locationId))
            {
                continue;
            }

            if (ForceLooseLootSpawnpointToTpl(spawnpoint, AdvancedElectronicMaterialsTpl))
            {
                patchedCount++;
            }
        }

        return patchedCount;
    }

    private int AddQuestAemLooseSpawnpointFallback()
    {
        var forcedSpawnDirectory = GetModPath("db", "QuestGatedForcedSpawnpoints");
        var spawnPath = System.IO.Path.Combine(forcedSpawnDirectory, "customSpawnpointsForced.json");

        if (!System.IO.File.Exists(spawnPath))
        {
            return 0;
        }

        var lighthouse = FindLocationObject(databaseService.GetLocations(), "lighthouse");

        if (lighthouse is null)
        {
            logger.Warning("ARC-186: Lighthouse location was not found for quest AEM loose-loot fallback.");
            return 0;
        }

        var spawnpoints = GetLooseLootSpawnpoints(lighthouse);

        if (spawnpoints is not IEnumerable enumerable)
        {
            logger.Warning("ARC-186: Lighthouse loose-loot spawnpoints were not found for quest AEM fallback.");
            return 0;
        }

        if (enumerable.Cast<object>().Any(spawnpoint =>
            IsQuestAemSpawnpoint(spawnpoint, GetStringMember(spawnpoint, "locationId", "LocationId", "id", "Id"))))
        {
            return 0;
        }

        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(spawnPath));

        if (!document.RootElement.TryGetProperty("lighthouse", out var lighthouseSpawns)
            || lighthouseSpawns.ValueKind != JsonValueKind.Array
            || lighthouseSpawns.GetArrayLength() == 0)
        {
            return 0;
        }

        var spawnpoint = CreateCollectionObjectFromJson(spawnpoints, lighthouseSpawns[0]);

        if (spawnpoint is null)
        {
            logger.Warning("ARC-186: Could not create quest AEM loose-loot fallback spawnpoint.");
            return 0;
        }

        AddObjectToCollection(spawnpoints, spawnpoint);
        return 1;
    }

    private int SuppressQuestAemLighthouseLooseSpawns()
    {
        var lighthouse = FindLocationObject(databaseService.GetLocations(), "lighthouse");

        if (lighthouse is null)
        {
            logger.Warning("ARC-186: Lighthouse location was not found for quest AEM loose-loot suppression.");
            return 0;
        }

        var spawnpoints = GetLooseLootSpawnpoints(lighthouse) as IEnumerable;

        if (spawnpoints is null)
        {
            logger.Warning("ARC-186: Lighthouse loose-loot spawnpoints were not found for quest AEM suppression.");
            return 0;
        }

        var patchedCount = 0;

        foreach (var spawnpoint in spawnpoints.Cast<object>())
        {
            var locationId = GetStringMember(spawnpoint, "locationId", "LocationId", "id", "Id");

            if (!IsQuestAemSpawnpoint(spawnpoint, locationId))
            {
                continue;
            }

            if (SetNumericMember(spawnpoint, 0d, "probability", "Probability"))
            {
                patchedCount++;
            }
        }

        return patchedCount;
    }

    private int RemoveQuestAemLighthouseLooseSpawns()
    {
        var lighthouse = FindLocationObject(databaseService.GetLocations(), "lighthouse");

        if (lighthouse is null)
        {
            logger.Warning("ARC-186: Lighthouse location was not found for quest AEM loose-loot removal.");
            return 0;
        }

        var spawnpoints = GetLooseLootSpawnpoints(lighthouse);

        if (spawnpoints is not IEnumerable enumerable)
        {
            logger.Warning("ARC-186: Lighthouse loose-loot spawnpoints were not found for quest AEM removal.");
            return 0;
        }

        var removedCount = 0;

        foreach (var spawnpoint in enumerable.Cast<object>().ToList())
        {
            var locationId = GetStringMember(spawnpoint, "locationId", "LocationId", "id", "Id");

            if (!IsQuestAemSpawnpoint(spawnpoint, locationId))
            {
                continue;
            }

            if (RemoveObjectFromCollection(spawnpoints, spawnpoint))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    private bool IsIntegratedAvionicsPartTwoCompleteForAnyLoadedProfile()
    {
        try
        {
            foreach (var profile in profileHelper.GetProfiles().Values)
            {
                var characters = GetMemberValue(profile, "Characters", "characters");
                var pmc = GetMemberValue(characters, "Pmc", "pmc");

                if (IsQuestComplete(pmc, IntegratedAvionicsPartTwoQuestId))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return IsIntegratedAvionicsPartTwoCompleteInProfileFiles();
    }

    private bool IsIntegratedAvionicsAemRecoveryWindowOpenForAnyLoadedProfile()
    {
        try
        {
            foreach (var profile in profileHelper.GetProfiles().Values)
            {
                var characters = GetMemberValue(profile, "Characters", "characters");
                var pmc = GetMemberValue(characters, "Pmc", "pmc");

                if (IsQuestComplete(pmc, IntegratedAvionicsPartTwoQuestId))
                {
                    continue;
                }

                if (IsQuestStartedOrComplete(pmc, IntegratedAvionicsPartOneQuestId))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return IsIntegratedAvionicsAemRecoveryWindowOpenInProfileFiles();
    }

    private bool IsIntegratedAvionicsPartTwoCompleteInProfileFiles()
    {
        var modRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (modRoot is null)
        {
            return false;
        }

        var profilesDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(modRoot, "..", "..", "profiles"));

        if (!System.IO.Directory.Exists(profilesDirectory))
        {
            return false;
        }

        foreach (var profilePath in System.IO.Directory.EnumerateFiles(profilesDirectory, "*.json"))
        {
            try
            {
                using var document = JsonDocument.Parse(System.IO.File.ReadAllText(profilePath));

                if (IsIntegratedAvionicsPartTwoComplete(document.RootElement))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private bool IsIntegratedAvionicsAemRecoveryWindowOpenInProfileFiles()
    {
        var modRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (modRoot is null)
        {
            return false;
        }

        var profilesDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(modRoot, "..", "..", "profiles"));

        if (!System.IO.Directory.Exists(profilesDirectory))
        {
            return false;
        }

        foreach (var profilePath in System.IO.Directory.EnumerateFiles(profilesDirectory, "*.json"))
        {
            try
            {
                using var document = JsonDocument.Parse(System.IO.File.ReadAllText(profilePath));

                if (IsIntegratedAvionicsPartTwoComplete(document.RootElement))
                {
                    continue;
                }

                if (IsQuestStartedOrComplete(document.RootElement, IntegratedAvionicsPartOneQuestId))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private static bool IsIntegratedAvionicsPartTwoComplete(JsonElement profile)
    {
        if (!TryGetJsonProperty(profile, out var quests, "characters", "pmc", "Quests")
            && !TryGetJsonProperty(profile, out quests, "characters", "pmc", "quests"))
        {
            return false;
        }

        if (quests.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var quest in quests.EnumerateArray())
        {
            if (!TryGetJsonProperty(quest, out var qid, "qid")
                || qid.ValueKind != JsonValueKind.String
                || !string.Equals(qid.GetString(), IntegratedAvionicsPartTwoQuestId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return TryGetJsonProperty(quest, out var status, "status")
                && status.ValueKind == JsonValueKind.Number
                && status.TryGetInt32(out var questStatus)
                && questStatus >= 4;
        }

        return false;
    }

    private static bool IsQuestStartedOrComplete(JsonElement profile, string questId)
    {
        if (!TryGetJsonProperty(profile, out var quests, "characters", "pmc", "Quests")
            && !TryGetJsonProperty(profile, out quests, "characters", "pmc", "quests"))
        {
            return false;
        }

        if (quests.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var quest in quests.EnumerateArray())
        {
            if (!TryGetJsonProperty(quest, out var qid, "qid")
                || qid.ValueKind != JsonValueKind.String
                || !string.Equals(qid.GetString(), questId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return TryGetJsonProperty(quest, out var status, "status")
                && status.ValueKind == JsonValueKind.Number
                && status.TryGetInt32(out var questStatus)
                && questStatus >= 2;
        }

        return false;
    }

    private static bool IsQuestAemSpawnpoint(object spawnpoint, string? locationId)
    {
        if (locationId is not null && LighthouseQuestAemSpawnpointIds.Contains(locationId))
        {
            return true;
        }

        if (!LooseLootSpawnpointContainsTpl(spawnpoint, AdvancedElectronicMaterialsTpl))
        {
            return false;
        }

        var position = GetMemberValue(GetMemberValue(spawnpoint, "template", "Template"), "Position", "position");

        if (position is null)
        {
            return false;
        }

        var x = GetNumericMember(position, "x", "X");
        var y = GetNumericMember(position, "y", "Y");
        var z = GetNumericMember(position, "z", "Z");

        if (x == 0d && y == 0d && z == 0d)
        {
            return false;
        }

        var distance = Math.Sqrt(
            Math.Pow(x - QuestAemTrainWarehouseX, 2d)
            + Math.Pow(y - QuestAemTrainWarehouseY, 2d)
            + Math.Pow(z - QuestAemTrainWarehouseZ, 2d));

        return distance <= QuestAemTrainWarehouseRadius;
    }

    private static bool LooseLootSpawnpointContainsTpl(object spawnpoint, string tpl)
    {
        var template = GetMemberValue(spawnpoint, "template", "Template");
        var items = GetMemberValue(template, "Items", "items") as IEnumerable;

        return items is not null && items.Cast<object>().Any(item => GetTpl(item) == tpl);
    }

    private static bool ForceLooseLootSpawnpointToTpl(object spawnpoint, string tpl)
    {
        var template = GetMemberValue(spawnpoint, "template", "Template");
        var items = GetMemberValue(template, "Items", "items") as IEnumerable;
        var itemDistribution = GetMemberValue(spawnpoint, "itemDistribution", "ItemDistribution");

        if (items is null || itemDistribution is null)
        {
            return false;
        }

        var targetIndex = -1;
        var index = 0;

        foreach (var item in items.Cast<object>())
        {
            if (GetTpl(item) == tpl)
            {
                targetIndex = index;
                break;
            }

            index++;
        }

        if (targetIndex < 0)
        {
            return false;
        }

        var patchedDistribution = false;
        var distributionEntries = itemDistribution is IDictionary dictionary
            ? dictionary.Values.Cast<object>()
            : itemDistribution is IEnumerable enumerable && itemDistribution is not string
                ? enumerable.Cast<object>()
                : Enumerable.Empty<object>();
        var distributionIndex = 0;

        foreach (var entry in distributionEntries)
        {
            var weight = distributionIndex == targetIndex ? QuestAemLooseLootWeight : 0d;

            if (SetNumericMember(entry, weight, "relativeProbability", "RelativeProbability", "weight", "Weight", "probability", "Probability"))
            {
                patchedDistribution = true;
            }

            distributionIndex++;
        }

        var patchedSpawnChance = SetNumericMember(spawnpoint, 1d, "probability", "Probability");

        return patchedDistribution && patchedSpawnChance;
    }

    private static bool IsQuestComplete(object? pmcProfile, string questId)
    {
        var quests = GetMemberValue(pmcProfile, "Quests", "quests") as IEnumerable;

        if (quests is null)
        {
            return false;
        }

        foreach (var quest in quests.Cast<object>())
        {
            var qid = GetStringMember(quest, "QId", "qid", "Id", "id");

            if (!string.Equals(qid, questId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return GetNumericMember(quest, "Status", "status") >= 4d;
        }

        return false;
    }

    private static bool IsQuestStartedOrComplete(object? pmcProfile, string questId)
    {
        var quests = GetMemberValue(pmcProfile, "Quests", "quests") as IEnumerable;

        if (quests is null)
        {
            return false;
        }

        foreach (var quest in quests.Cast<object>())
        {
            var qid = GetStringMember(quest, "QId", "qid", "Id", "id");

            if (!string.Equals(qid, questId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return GetNumericMember(quest, "Status", "status") >= 2d;
        }

        return false;
    }

    private void EnsureArc186HideoutRecipeRegisteredFromJson()
    {
        var recipePath = GetModPath("db", "CustomHideoutRecipes", "arc186.json");

        if (!System.IO.File.Exists(recipePath))
        {
            return;
        }

        var recipes = databaseService.GetHideout().Production.Recipes;

        if (recipes is null)
        {
            logger.Warning("ARC-186: Hideout production recipe list was not found.");
            return;
        }

        if (IsArc186HideoutRecipeRegistered())
        {
            return;
        }

        var recipe = jsonUtil.DeserializeFromFile(recipePath, GetCollectionElementType(recipes.GetType()));

        if (recipe is null)
        {
            logger.Warning("ARC-186: Hideout recipe file exists but could not be loaded.");
            return;
        }

        AddObjectToCollection(recipes, recipe);
    }

    private bool IsArc186HideoutRecipeRegistered()
    {
        var recipes = databaseService.GetHideout().Production.Recipes;

        return recipes is not null && recipes.Any(recipe => GetStringMember(recipe, "Id", "_id") == "6a0b24653f221925eea85489");
    }

    private int AddArc186ToBirdeyeGuaranteedLoot()
    {
        var birdeye = GetBotType(databaseService.GetBots(), BirdeyeBotType);

        if (birdeye is null)
        {
            logger.Warning("ARC-186: Birdeye bot data was not found.");
            return 0;
        }

        var botInventory = GetMemberValue(birdeye, "BotInventory", "botInventory", "Inventory", "inventory");
        var itemPools = GetMemberValue(botInventory, "Items", "items");
        var backpack = GetMemberValue(itemPools, "Backpack", "backpack");

        if (backpack is null)
        {
            logger.Warning("ARC-186: Birdeye backpack loot pool was not found.");
            return 0;
        }

        return ForceWeightDictionaryToSingleTpl(backpack, Arc186Tpl, BirdeyeArc186BackpackWeight) ? 1 : 0;
    }

    private int AddBotBackpackLootFromConfig()
    {
        var patchedCount = 0;
        var botLoadoutDirectory = GetModPath("db", "CustomBotLoadouts");

        if (!System.IO.Directory.Exists(botLoadoutDirectory))
        {
            return AddArc186ToBirdeyeGuaranteedLoot();
        }

        foreach (var loadoutPath in System.IO.Directory.EnumerateFiles(botLoadoutDirectory, "*.json"))
        {
            var botType = System.IO.Path.GetFileNameWithoutExtension(loadoutPath);
            var bot = GetBotType(databaseService.GetBots(), botType);

            if (bot is null)
            {
                logger.Warning($"ARC-186: Bot loadout config {System.IO.Path.GetFileName(loadoutPath)} could not find bot type {botType}.");
                continue;
            }

            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(loadoutPath));

            if (!TryGetJsonProperty(document.RootElement, out var backpackConfig, "inventory", "items", "Backpack"))
            {
                continue;
            }

            var botInventory = GetMemberValue(bot, "BotInventory", "botInventory", "Inventory", "inventory");
            var itemPools = GetMemberValue(botInventory, "Items", "items");
            var backpackPool = GetMemberValue(itemPools, "Backpack", "backpack");

            if (backpackPool is null)
            {
                logger.Warning($"ARC-186: Backpack loot pool was not found for bot type {botType}.");
                continue;
            }

            foreach (var entry in backpackConfig.EnumerateObject())
            {
                var weight = entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetDouble(out var configuredWeight)
                    ? configuredWeight
                    : 1d;

                if (AddTplWeightToDictionary(backpackPool, entry.Name, weight))
                {
                    patchedCount++;
                }
            }

            if (string.Equals(botType, BirdeyeBotType, StringComparison.OrdinalIgnoreCase)
                && backpackConfig.TryGetProperty(Arc186Tpl, out var arc186WeightElement))
            {
                var weight = arc186WeightElement.ValueKind == JsonValueKind.Number && arc186WeightElement.TryGetDouble(out var configuredWeight)
                    ? configuredWeight
                    : BirdeyeArc186BackpackWeight;

                if (ForceWeightDictionaryToSingleTpl(backpackPool, Arc186Tpl, weight))
                {
                    patchedCount++;
                }
            }
        }

        return patchedCount;
    }

    private int AddBotBackpackLoadoutFromConfig()
    {
        var patchedCount = AddBotBackpackLootFromConfig();
        var botLoadoutDirectory = GetModPath("db", "CustomBotLoadouts");

        if (!System.IO.Directory.Exists(botLoadoutDirectory))
        {
            return patchedCount;
        }

        foreach (var loadoutPath in System.IO.Directory.EnumerateFiles(botLoadoutDirectory, "*.json"))
        {
            var botType = System.IO.Path.GetFileNameWithoutExtension(loadoutPath);
            var bot = GetBotType(databaseService.GetBots(), botType);

            if (bot is null)
            {
                continue;
            }

            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(loadoutPath));
            patchedCount += PatchBackpackEquipmentChance(bot, document.RootElement);
            patchedCount += PatchBackpackEquipmentPool(bot, botType, document.RootElement);
        }

        return patchedCount;
    }

    private static int PatchBackpackEquipmentChance(object bot, JsonElement loadoutRoot)
    {
        if (!TryGetJsonProperty(loadoutRoot, out var chanceConfig, "chances", "equipment", "Backpack"))
        {
            return 0;
        }

        var chance = chanceConfig.ValueKind == JsonValueKind.Number && chanceConfig.TryGetDouble(out var configuredChance)
            ? configuredChance
            : 100d;
        var chances = GetMemberValue(bot, "Chances", "chances");
        var equipmentChances = GetMemberValue(chances, "Equipment", "equipment");

        return equipmentChances is not null && AddTplWeightToDictionary(equipmentChances, "Backpack", chance) ? 1 : 0;
    }

    private static int PatchBackpackEquipmentPool(object bot, string botType, JsonElement loadoutRoot)
    {
        if (!TryGetJsonProperty(loadoutRoot, out var equipmentConfig, "inventory", "equipment", "Backpack"))
        {
            return 0;
        }

        var botInventory = GetMemberValue(bot, "BotInventory", "botInventory", "Inventory", "inventory");
        var equipmentPools = GetMemberValue(botInventory, "Equipment", "equipment");
        var backpackEquipmentPool = GetMemberValue(equipmentPools, "Backpack", "backpack");

        if (backpackEquipmentPool is null)
        {
            return 0;
        }

        if (string.Equals(botType, BirdeyeBotType, StringComparison.OrdinalIgnoreCase)
            && equipmentConfig.TryGetProperty(BirdeyeComm3BackpackTpl, out var comm3WeightElement))
        {
            var weight = comm3WeightElement.ValueKind == JsonValueKind.Number && comm3WeightElement.TryGetDouble(out var configuredWeight)
                ? configuredWeight
                : BirdeyeArc186BackpackWeight;

            return ForceWeightDictionaryToSingleTpl(backpackEquipmentPool, BirdeyeComm3BackpackTpl, weight) ? 1 : 0;
        }

        var patchedCount = 0;

        foreach (var entry in equipmentConfig.EnumerateObject())
        {
            var weight = entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetDouble(out var configuredWeight)
                ? configuredWeight
                : 1d;

            if (AddTplWeightToDictionary(backpackEquipmentPool, entry.Name, weight))
            {
                patchedCount++;
            }
        }

        return patchedCount;
    }

    private int EnsureBirdeyeBackpackLootRolls()
    {
        var birdeye = GetBotType(databaseService.GetBots(), BirdeyeBotType);

        if (birdeye is null)
        {
            logger.Warning("ARC-186: Birdeye bot data was not found while patching backpack loot rolls.");
            return 0;
        }

        var generation = GetMemberValue(birdeye, "Generation", "generation");
        var items = GetMemberValue(generation, "Items", "items");
        var backpackLoot = GetMemberValue(items, "BackpackLoot", "backpackLoot");
        var weights = GetMemberValue(backpackLoot, "Weights", "weights")
            ?? FindNestedMemberValue(birdeye, ["BackpackLoot", "backpackLoot"], ["Weights", "weights"]);

        if (weights is null)
        {
            logger.Warning("ARC-186: Birdeye backpack loot roll weights were not found.");
            return 0;
        }

        var patchedCount = 0;

        if (ForceWeightDictionaryToSingleTpl(weights, "1", BirdeyeBackpackSingleLootRollWeight))
        {
            patchedCount++;
        }

        var whitelist = GetMemberValue(backpackLoot, "Whitelist", "whitelist")
            ?? FindNestedMemberValue(birdeye, ["BackpackLoot", "backpackLoot"], ["Whitelist", "whitelist"]);

        if (whitelist is IEnumerable whitelistCollection
            && whitelistCollection is not string
            && !whitelistCollection.Cast<object>().Any(value => value.ToString() == Arc186Tpl))
        {
            AddValueToCollection(whitelistCollection, Arc186Tpl);
            patchedCount++;
        }

        return patchedCount;
    }

    private int AddForcedSpawnpointsFromConfig()
    {
        var forcedSpawnDirectory = GetModPath("db", "QuestGatedForcedSpawnpoints");

        if (!System.IO.Directory.Exists(forcedSpawnDirectory))
        {
            return 0;
        }

        var patchedCount = 0;

        foreach (var spawnPath in System.IO.Directory.EnumerateFiles(forcedSpawnDirectory, "*.json"))
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(spawnPath));

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                logger.Warning($"ARC-186: Forced spawn config {System.IO.Path.GetFileName(spawnPath)} must be a location object.");
                continue;
            }

            foreach (var locationConfig in document.RootElement.EnumerateObject())
            {
                var location = FindLocationObject(databaseService.GetLocations(), locationConfig.Name);

                if (location is null)
                {
                    logger.Warning($"ARC-186: Forced spawn location {locationConfig.Name} was not found.");
                    continue;
                }

                var spawnpoints = GetLooseLootForcedSpawnpoints(location) ?? GetLooseLootSpawnpoints(location);

                if (spawnpoints is null)
                {
                    logger.Warning($"ARC-186: Forced spawn location {locationConfig.Name} has no loose loot spawnpoint collection.");
                    continue;
                }

                foreach (var spawnpointJson in locationConfig.Value.EnumerateArray())
                {
                    var locationId = spawnpointJson.TryGetProperty("locationId", out var locationIdElement)
                        && locationIdElement.ValueKind == JsonValueKind.String
                            ? locationIdElement.GetString()
                            : null;

                    if (!string.IsNullOrWhiteSpace(locationId)
                        && SpawnpointCollectionContainsLocationId(spawnpoints, locationId))
                    {
                        continue;
                    }

                    var spawnpoint = CreateCollectionObjectFromJson(spawnpoints, spawnpointJson);

                    if (spawnpoint is null)
                    {
                        logger.Warning($"ARC-186: Could not create forced spawnpoint for {locationConfig.Name}.");
                        continue;
                    }

                    AddObjectToCollection(spawnpoints, spawnpoint);
                    patchedCount++;
                }
            }
        }

        return patchedCount;
    }

    private static bool SpawnpointCollectionContainsLocationId(object spawnpoints, string locationId)
    {
        return spawnpoints is IEnumerable enumerable
            && enumerable.Cast<object>().Any(spawnpoint =>
                string.Equals(GetStringMember(spawnpoint, "locationId", "LocationId", "id", "Id"), locationId, StringComparison.OrdinalIgnoreCase));
    }

    private static object? FindLocationObject(object locations, string locationName)
    {
        if (TryGetDictionaryValue(locations, locationName, out var directLocation))
        {
            return directLocation;
        }

        foreach (var location in EnumerateNamedLocationObjects(locations))
        {
            if (location.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase)
                || GetStringMember(location.Value, "Id", "id", "_id")?.Equals(locationName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return location.Value;
            }
        }

        return null;
    }

    private static object? GetLooseLootSpawnpoints(object location)
    {
        var looseLoot = GetMemberValue(location, "LooseLoot", "looseLoot");
        var looseLootValue = GetMemberValue(looseLoot, "Value") ?? looseLoot;

        return GetMemberValue(looseLootValue, "Spawnpoints", "spawnpoints", "SpawnPoints", "spawnPoints");
    }

    private static object? GetLooseLootForcedSpawnpoints(object location)
    {
        var looseLoot = GetMemberValue(location, "LooseLoot", "looseLoot");
        var looseLootValue = GetMemberValue(looseLoot, "Value") ?? looseLoot;

        return GetMemberValue(looseLootValue, "SpawnpointsForced", "spawnpointsForced", "ForcedSpawnpoints", "forcedSpawnpoints");
    }

    private static object? CreateCollectionObjectFromJson(object collection, JsonElement element)
    {
        var elementType = GetCollectionElementType(collection.GetType());

        if (elementType == typeof(object))
        {
            return ConvertJsonElement(element, typeof(object));
        }

        var instance = Activator.CreateInstance(elementType);

        if (instance is null)
        {
            return null;
        }

        return PopulateObjectFromJson(instance, element) ? instance : null;
    }

    private static bool PopulateObjectFromJson(object target, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!SetMemberValueFromJson(target, property.Name, property.Value))
            {
                SetExtensionDataJsonValue(target, property.Name, property.Value);
            }
        }

        return true;
    }

    private static bool SetMemberValueFromJson(object source, string name, JsonElement value)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var property = type.GetProperty(name, flags);

        if (property is not null && property.CanWrite)
        {
            property.SetValue(source, ConvertJsonElement(value, property.PropertyType));
            return true;
        }

        var field = type.GetField(name, flags);

        if (field is not null)
        {
            field.SetValue(source, ConvertJsonElement(value, field.FieldType));
            return true;
        }

        return false;
    }

    private static object? ConvertJsonElement(JsonElement value, Type targetType)
    {
        var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (conversionType == typeof(object))
        {
            return ConvertJsonElementToPlainValue(value);
        }

        if (conversionType == typeof(string))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        if (conversionType == typeof(bool))
        {
            return value.GetBoolean();
        }

        if (conversionType.IsEnum)
        {
            return value.ValueKind == JsonValueKind.String
                ? Enum.Parse(conversionType, value.GetString() ?? string.Empty, true)
                : Enum.ToObject(conversionType, value.GetInt32());
        }

        if (conversionType.FullName == "SPTarkov.Server.Core.Models.Common.MongoId" && value.ValueKind == JsonValueKind.String)
        {
            return CreateMongoId(conversionType, value.GetString() ?? string.Empty);
        }

        if (conversionType == typeof(int)
            || conversionType == typeof(long)
            || conversionType == typeof(short)
            || conversionType == typeof(byte)
            || conversionType == typeof(uint)
            || conversionType == typeof(ulong)
            || conversionType == typeof(float)
            || conversionType == typeof(double)
            || conversionType == typeof(decimal))
        {
            return Convert.ChangeType(value.GetDouble(), conversionType, CultureInfo.InvariantCulture);
        }

        if (typeof(IDictionary).IsAssignableFrom(conversionType) && value.ValueKind == JsonValueKind.Object)
        {
            return CreateDictionaryFromJson(conversionType, value);
        }

        if (conversionType.IsArray && value.ValueKind == JsonValueKind.Array)
        {
            var itemType = conversionType.GetElementType() ?? typeof(object);
            var values = value.EnumerateArray().Select(item => ConvertJsonElement(item, itemType)).ToArray();
            var array = Array.CreateInstance(itemType, values.Length);

            for (var i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        if (typeof(IEnumerable).IsAssignableFrom(conversionType) && conversionType != typeof(string) && value.ValueKind == JsonValueKind.Array)
        {
            return CreateCollectionFromJson(conversionType, value);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var nestedObject = Activator.CreateInstance(conversionType);

            if (nestedObject is not null && PopulateObjectFromJson(nestedObject, value))
            {
                return nestedObject;
            }
        }

        return value.ToString();
    }

    private static object? ConvertJsonElementToPlainValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonElementToPlainValue).ToList(),
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name, property => ConvertJsonElementToPlainValue(property.Value)),
            _ => value.ToString()
        };
    }

    private static object CreateDictionaryFromJson(Type dictionaryType, JsonElement value)
    {
        var genericArguments = dictionaryType.IsGenericType ? dictionaryType.GetGenericArguments() : [typeof(string), typeof(object)];
        var keyType = genericArguments[0];
        var valueType = genericArguments[1];
        var concreteType = dictionaryType.IsInterface || dictionaryType.IsAbstract
            ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType)
            : dictionaryType;
        var dictionary = (IDictionary)(Activator.CreateInstance(concreteType)
            ?? throw new InvalidOperationException("Could not create dictionary."));

        foreach (var property in value.EnumerateObject())
        {
            dictionary[CreateValue(keyType, property.Name)] = ConvertJsonElement(property.Value, valueType);
        }

        return dictionary;
    }

    private static object CreateCollectionFromJson(Type collectionType, JsonElement value)
    {
        var itemType = GetCollectionElementType(collectionType);
        var concreteType = collectionType.IsInterface || collectionType.IsAbstract
            ? typeof(List<>).MakeGenericType(itemType)
            : collectionType;
        var collection = Activator.CreateInstance(concreteType)
            ?? throw new InvalidOperationException("Could not create collection.");

        foreach (var item in value.EnumerateArray())
        {
            AddObjectToCollection(collection, ConvertJsonElement(item, itemType) ?? string.Empty);
        }

        return collection;
    }

    private static void SetExtensionDataJsonValue(object source, string name, JsonElement value)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var extensionData = type.GetProperty("ExtensionData", flags)?.GetValue(source)
            ?? type.GetField("_extensionData", flags)?.GetValue(source);

        if (extensionData is IDictionary dictionary)
        {
            dictionary[name] = ConvertJsonElementToPlainValue(value);
        }
    }

    private static bool TryGetJsonProperty(JsonElement source, out JsonElement value, params string[] names)
    {
        value = source;

        foreach (var name in names)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out value))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetModPath(params string[] parts)
    {
        var modRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;

        return System.IO.Path.Combine(new[] { modRoot }.Concat(parts).ToArray());
    }

    private void LogStartupMessage()
    {
        logger.Info("ARC-186 loaded.");

        var messages = new[]
        {
            "Guard channel open. Guard frequency 243.000 MHz.",
            "3-level spotted unsupervised on the flightline. Somebody find a 7-level.",
            "QA inbound. Hide the white Monster."
        };

        logger.Info($"ARC-186: {messages[RandomNumberGenerator.GetInt32(messages.Length)]}");
    }

    private static IEnumerable<object> EnumerateLocationObjects(object locations)
    {
        foreach (var location in EnumerateNamedLocationObjects(locations))
        {
            yield return location.Value;
        }
    }

    private static IEnumerable<(string Name, object Value)> EnumerateNamedLocationObjects(object locations)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var property in locations.GetType().GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value;

            try
            {
                value = property.GetValue(locations);
            }
            catch
            {
                continue;
            }

            if (value is not null && value is not string && !IsSimpleValue(value))
            {
                yield return (property.Name, value);
            }
        }

        foreach (var field in locations.GetType().GetFields(flags))
        {
            var value = field.GetValue(locations);

            if (value is not null && value is not string && !IsSimpleValue(value))
            {
                yield return (field.Name, value);
            }
        }
    }

    private static object? GetBotType(object bots, string botType)
    {
        if (TryGetDictionaryValue(bots, botType, out var directValue))
        {
            return directValue;
        }

        var containers = new[]
        {
            GetMemberValue(bots, "Types", "types"),
            GetMemberValue(bots, "BotTypes", "botTypes"),
            GetMemberValue(bots, "Bots", "bots")
        };

        foreach (var container in containers)
        {
            if (container is not null && TryGetDictionaryValue(container, botType, out var containerValue))
            {
                return containerValue;
            }
        }

        return GetMemberValue(bots, "BossBoar", "bossBoar", "bossboar");
    }

    private static string GetFriendlyLocationName(string locationName)
    {
        return locationName.ToLowerInvariant() switch
        {
            "bigmap" => "Customs",
            "woods" => "Woods",
            "shoreline" => "Shoreline",
            "lighthouse" => "Lighthouse",
            "rezervbase" => "Reserve",
            "interchange" => "Interchange",
            "factory4_day" => "Factory day",
            "factory4_night" => "Factory night",
            "laboratory" => "Labs",
            "tarkovstreets" => "Streets",
            "sandbox" => "Ground Zero",
            "sandbox_high" => "Ground Zero high",
            _ => locationName
        };
    }

    private static bool TryGetDictionaryValue(object source, string key, out object? value)
    {
        value = null;

        if (source is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonElement.EnumerateObject())
                {
                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = UnwrapJsonElement(property.Value);
                        return true;
                    }
                }
            }

            return false;
        }

        if (source is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key?.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
                {
                    value = entry.Value;
                    return true;
                }
            }

            return false;
        }

        if (source is not IEnumerable enumerable || source is string)
        {
            return false;
        }

        foreach (var entry in enumerable)
        {
            var entryKey = GetMemberValue(entry, "Key");

            if (entryKey?.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
            {
                value = GetMemberValue(entry, "Value") ?? entry;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(string Key, object Value)> EnumerateDictionaryEntries(object source)
    {
        if (source is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not null && entry.Value is not null)
                {
                    yield return (entry.Key.ToString() ?? string.Empty, entry.Value);
                }
            }

            yield break;
        }

        if (source is IEnumerable enumerable && source is not string)
        {
            foreach (var entry in enumerable.Cast<object>())
            {
                var key = GetMemberValue(entry, "Key")?.ToString();
                var value = GetMemberValue(entry, "Value") ?? entry;

                if (key is not null)
                {
                    yield return (key, value);
                }
            }
        }
    }

    private static object? UnwrapJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value
        };
    }

    private static void AddTplToSlotFilters(object slot, string tpl)
    {
        var slotProperties = GetMemberValue(slot, "_props", "Props", "Properties");
        var filters = GetMemberValue(slotProperties, "filters", "Filters") as IEnumerable;

        if (filters is null)
        {
            return;
        }

        foreach (var filter in filters)
        {
            var acceptedTpls = GetMemberValue(filter, "Filter") as IEnumerable;

            if (acceptedTpls is null || acceptedTpls.Cast<object>().Any(value => value.ToString() == tpl))
            {
                continue;
            }

            AddValueToCollection(acceptedTpls, tpl);
        }
    }

    private static void AddTplToPosterSlotFilters(object slot, HashSet<string> posterTpls, string tpl)
    {
        var slotName = GetStringMember(slot, "_name", "Name");
        var slotProperties = GetMemberValue(slot, "_props", "Props", "Properties");
        var filters = GetMemberValue(slotProperties, "filters", "Filters") as IEnumerable;

        if (filters is null)
        {
            return;
        }

        foreach (var filter in filters)
        {
            var acceptedTpls = GetMemberValue(filter, "Filter") as IEnumerable;

            if (acceptedTpls is null)
            {
                continue;
            }

            var acceptedList = acceptedTpls.Cast<object>().Select(value => value.ToString()).ToList();
            var isPosterSlot = slotName?.Contains("Poster", StringComparison.OrdinalIgnoreCase) == true;
            var acceptsPoster = acceptedList.Any(value => value is not null && posterTpls.Contains(value));

            if ((!isPosterSlot && !acceptsPoster) || acceptedList.Contains(tpl))
            {
                continue;
            }

            AddValueToCollection(acceptedTpls, tpl);
        }
    }

    private static bool AddWeightedTplToCollection(IEnumerable collection, object referenceEntry, string tpl, double weight)
    {
        var entry = CreateWeightedEntry(referenceEntry, tpl, weight);

        if (entry is null)
        {
            return false;
        }

        AddObjectToCollection(collection, entry);
        return true;
    }

    private static object? CreateWeightedEntry(object referenceEntry, string tpl, double weight)
    {
        var entryType = referenceEntry.GetType();
        var entry = Activator.CreateInstance(entryType);

        if (entry is null)
        {
            return null;
        }

        CopyWritableMembers(referenceEntry, entry);

        if (!SetMemberValue(entry, tpl, "tpl", "Tpl", "_tpl", "TemplateId", "ItemTpl", "itemTpl"))
        {
            return null;
        }

        return SetNumericMember(entry, weight, "relativeProbability", "RelativeProbability", "weight", "Weight", "probability", "Probability")
            ? entry
            : null;
    }

    private static void CopyWritableMembers(object source, object target)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var property in source.GetType().GetProperties(flags))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            property.SetValue(target, property.GetValue(source));
        }

        foreach (var field in source.GetType().GetFields(flags))
        {
            if (field.IsInitOnly)
            {
                continue;
            }

            field.SetValue(target, field.GetValue(source));
        }
    }

    private static IEnumerable<object> EnumerateDatabaseValues(object databaseItems)
    {
        if (databaseItems is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is not null)
                {
                    yield return entry.Value;
                }
            }

            yield break;
        }

        if (databaseItems is IEnumerable enumerable && databaseItems is not string)
        {
            foreach (var entry in enumerable)
            {
                if (entry is null)
                {
                    continue;
                }

                var valueProperty = entry.GetType().GetProperty("Value");
                yield return valueProperty?.GetValue(entry) ?? entry;
            }

            yield break;
        }

        var value = GetMemberValue(databaseItems, "Value");

        if (value is not null)
        {
            foreach (var entry in EnumerateDatabaseValues(value))
            {
                yield return entry;
            }
        }
    }

    private static bool ContainsTpl(object source, string tpl)
    {
        if (source is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key?.ToString() == tpl || entry.Value?.ToString() == tpl)
                {
                    return true;
                }
            }

            return false;
        }

        if (source is IEnumerable enumerable && source is not string)
        {
            return enumerable.Cast<object>().Any(value => value.ToString() == tpl || GetTpl(value) == tpl);
        }

        return source.ToString() == tpl || GetTpl(source) == tpl;
    }

    private static string? GetTpl(object? source)
    {
        return GetStringMember(source, "tpl", "Tpl", "_tpl", "TemplateId", "ItemTpl", "itemTpl");
    }

    private static void AddValueToCollection(object collection, string value)
    {
        var collectionType = collection.GetType();
        var elementType = GetCollectionElementType(collectionType);
        var typedValue = CreateValue(elementType, value);

        var addMethod = collectionType.GetMethod("Add", [elementType]);

        addMethod?.Invoke(collection, [typedValue]);
    }

    private static void AddObjectToCollection(object collection, object value)
    {
        var collectionType = collection.GetType();
        var elementType = GetCollectionElementType(collectionType);
        var typedValue = elementType.IsInstanceOfType(value) ? value : CreateValue(elementType, value.ToString() ?? string.Empty);

        var addMethod = collectionType.GetMethod("Add", [elementType]);

        addMethod?.Invoke(collection, [typedValue]);
    }

    private static object? CreateEmptyCollectionLike(object referenceCollection)
    {
        var collectionType = referenceCollection.GetType();
        var elementType = GetCollectionElementType(collectionType);
        var concreteType = collectionType.IsInterface || collectionType.IsAbstract
            ? typeof(List<>).MakeGenericType(elementType)
            : collectionType;

        return Activator.CreateInstance(concreteType);
    }

    private static bool AddTplWeightToDictionary(object dictionaryObject, string tpl, double weight)
    {
        var dictionaryType = dictionaryObject.GetType();
        var genericArguments = dictionaryType.GetGenericArguments();

        if (genericArguments.Length != 2)
        {
            return false;
        }

        var key = CreateValue(genericArguments[0], tpl);
        var valueType = Nullable.GetUnderlyingType(genericArguments[1]) ?? genericArguments[1];
        var value = Convert.ChangeType(weight, valueType);

        if (dictionaryObject is IDictionary dictionary)
        {
            dictionary[key] = value;
            return true;
        }

        var addMethod = dictionaryType.GetMethod("Add", genericArguments);

        if (addMethod is null)
        {
            return false;
        }

        addMethod.Invoke(dictionaryObject, [key, value]);
        return true;
    }

    private static bool ForceWeightDictionaryToSingleTpl(object dictionaryObject, string tpl, double weight)
    {
        var dictionaryType = dictionaryObject.GetType();
        var genericArguments = dictionaryType.GetGenericArguments();

        if (genericArguments.Length != 2)
        {
            return false;
        }

        var valueType = Nullable.GetUnderlyingType(genericArguments[1]) ?? genericArguments[1];

        if (dictionaryObject is not IDictionary dictionary)
        {
            return AddTplWeightToDictionary(dictionaryObject, tpl, weight);
        }

        var zeroValue = Convert.ChangeType(0d, valueType);
        var keys = dictionary.Keys.Cast<object>().ToList();

        foreach (var existingKey in keys)
        {
            dictionary[existingKey] = zeroValue;
        }

        dictionary[CreateValue(genericArguments[0], tpl)] = Convert.ChangeType(weight, valueType);
        return true;
    }

    private static bool ReplaceCollectionWithSingleTplItem(IEnumerable collection, object referenceItem, string itemId, string tpl)
    {
        var item = Activator.CreateInstance(referenceItem.GetType());

        if (item is null)
        {
            return false;
        }

        CopyWritableMembers(referenceItem, item);

        if (!SetMemberValue(item, itemId, "_id", "Id", "id")
            || !SetMemberValue(item, tpl, "_tpl", "Tpl", "tpl", "TemplateId", "itemTpl", "ItemTpl"))
        {
            return false;
        }

        if (!ClearCollection(collection))
        {
            return false;
        }

        AddObjectToCollection(collection, item);
        return true;
    }

    private static bool ClearCollection(object collection)
    {
        var clearMethod = collection.GetType().GetMethod("Clear", Type.EmptyTypes);

        if (clearMethod is null)
        {
            return false;
        }

        clearMethod.Invoke(collection, []);
        return true;
    }

    private static bool RemoveObjectFromCollection(object collection, object value)
    {
        if (collection is IList list)
        {
            list.Remove(value);
            return true;
        }

        var collectionType = collection.GetType();
        var removeMethod = collectionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
            {
                if (method.Name != "Remove")
                {
                    return false;
                }

                var parameters = method.GetParameters();

                return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(value);
            });

        if (removeMethod is null)
        {
            return false;
        }

        removeMethod.Invoke(collection, [value]);
        return true;
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            return collectionType.GetGenericArguments()[0];
        }

        var genericEnumerable = collectionType
            .GetInterfaces()
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return genericEnumerable?.GetGenericArguments()[0] ?? typeof(string);
    }

    private static object CreateValue(Type targetType, string value)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        var nullableType = Nullable.GetUnderlyingType(targetType);
        var conversionType = nullableType ?? targetType;

        if (conversionType.IsEnum)
        {
            return Enum.Parse(conversionType, value, true);
        }

        if (conversionType == typeof(Guid))
        {
            return Guid.Parse(value);
        }

        if (conversionType.FullName == "SPTarkov.Server.Core.Models.Common.MongoId" && IsMongoIdHex(value))
        {
            return CreateMongoId(conversionType, value);
        }

        if (conversionType == typeof(bool))
        {
            return bool.Parse(value);
        }

        if (conversionType == typeof(double)
            || conversionType == typeof(float)
            || conversionType == typeof(decimal)
            || conversionType == typeof(int)
            || conversionType == typeof(long)
            || conversionType == typeof(short)
            || conversionType == typeof(byte))
        {
            return Convert.ChangeType(value, conversionType);
        }

        var stringConstructor = targetType.GetConstructor([typeof(string)]);

        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke([value]);
        }

        return value;
    }

    private static bool IsMongoIdHex(string value)
    {
        return value.Length == 24 && Regex.IsMatch(value, "^[0-9a-fA-F]{24}$");
    }

    private static object CreateMongoId(Type mongoIdType, string value)
    {
        var mongoId = Activator.CreateInstance(mongoIdType)
            ?? throw new InvalidOperationException("Could not create MongoId value.");
        var bytes = Convert.FromHexString(value);
        var timestampBytes = bytes[..8];
        var pidBytes = bytes[8..12];
        Array.Reverse(timestampBytes);
        Array.Reverse(pidBytes);
        var timestampAndMachine = BitConverter.ToUInt64(timestampBytes, 0);
        var pidAndIncrement = BitConverter.ToUInt32(pidBytes, 0);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        SetIntegerField(mongoId, mongoIdType.GetField("_timestampAndMachine", flags), timestampAndMachine);
        SetIntegerField(mongoId, mongoIdType.GetField("_pidAndIncrement", flags), pidAndIncrement);

        return mongoId;
    }

    private static void SetIntegerField(object source, FieldInfo? field, ulong value)
    {
        if (field is null)
        {
            return;
        }

        var targetType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var convertedValue = targetType == typeof(long) ? unchecked((long)value)
            : targetType == typeof(int) ? unchecked((int)value)
            : targetType == typeof(uint) ? unchecked((uint)value)
            : targetType == typeof(ulong) ? value
            : Convert.ChangeType(value, targetType);

        field.SetValue(source, convertedValue);
    }

    private static bool SetMemberValue(object source, object value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);

            if (property is not null && property.CanWrite)
            {
                try
                {
                    property.SetValue(source, CreateValue(property.PropertyType, value.ToString() ?? string.Empty));
                    return true;
                }
                catch
                {
                    return SetExtensionDataValue(source, value, names);
                }
            }

            var field = type.GetField(name, flags);

            if (field is not null)
            {
                try
                {
                    field.SetValue(source, CreateValue(field.FieldType, value.ToString() ?? string.Empty));
                    return true;
                }
                catch
                {
                    return SetExtensionDataValue(source, value, names);
                }
            }
        }

        return SetExtensionDataValue(source, value, names);
    }

    private static bool SetRawMemberValue(object source, object value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);

            if (property is not null && property.CanWrite && property.PropertyType.IsInstanceOfType(value))
            {
                property.SetValue(source, value);
                return true;
            }

            var field = type.GetField(name, flags);

            if (field is not null && field.FieldType.IsInstanceOfType(value))
            {
                field.SetValue(source, value);
                return true;
            }
        }

        return false;
    }

    private static bool SetExtensionDataValue(object source, object value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var extensionData = type.GetProperty("ExtensionData", flags)?.GetValue(source)
            ?? type.GetField("_extensionData", flags)?.GetValue(source);

        if (extensionData is not IDictionary dictionary)
        {
            return false;
        }

        var existingKey = dictionary.Keys
            .Cast<object>()
            .FirstOrDefault(key => names.Any(name => key?.ToString()?.Equals(name, StringComparison.OrdinalIgnoreCase) == true));

        var wantedKey = existingKey ?? names[0];
        dictionary[wantedKey] = value;
        return true;
    }

    private static bool SetExtensionDataNumericValue(object source, double value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var extensionData = type.GetProperty("ExtensionData", flags)?.GetValue(source)
            ?? type.GetField("_extensionData", flags)?.GetValue(source);

        if (extensionData is not IDictionary dictionary)
        {
            return false;
        }

        var existingKey = dictionary.Keys
            .Cast<object>()
            .FirstOrDefault(key => names.Any(name => key?.ToString()?.Equals(name, StringComparison.OrdinalIgnoreCase) == true));

        var wantedKey = existingKey ?? names[0];
        dictionary[wantedKey] = value;
        return true;
    }

    private static bool SetExtensionDataBooleanValue(object source, bool value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var extensionData = type.GetProperty("ExtensionData", flags)?.GetValue(source)
            ?? type.GetField("_extensionData", flags)?.GetValue(source);

        if (extensionData is not IDictionary dictionary)
        {
            return false;
        }

        var existingKey = dictionary.Keys
            .Cast<object>()
            .FirstOrDefault(key => names.Any(name => key?.ToString()?.Equals(name, StringComparison.OrdinalIgnoreCase) == true));

        var wantedKey = existingKey ?? names[0];
        dictionary[wantedKey] = value;
        return true;
    }

    private static bool SetNumericMember(object source, double value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);

            if (property is not null && property.CanWrite)
            {
                property.SetValue(source, CreateNumericValue(property.PropertyType, value));
                return true;
            }

            var field = type.GetField(name, flags);

            if (field is not null)
            {
                field.SetValue(source, CreateNumericValue(field.FieldType, value));
                return true;
            }
        }

        return SetExtensionDataNumericValue(source, value, names);
    }

    private static object CreateNumericValue(Type targetType, double value)
    {
        var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return conversionType.IsEnum
            ? Enum.ToObject(conversionType, Convert.ToInt32(value))
            : Convert.ChangeType(value, conversionType);
    }

    private static bool SetBooleanMember(object source, bool value, params string[] names)
    {
        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);

            if (property is not null && property.CanWrite)
            {
                property.SetValue(source, value);
                return true;
            }

            var field = type.GetField(name, flags);

            if (field is not null)
            {
                field.SetValue(source, value);
                return true;
            }
        }

        return SetExtensionDataBooleanValue(source, value, names);
    }

    private static double GetNumericMember(object? source, params string[] names)
    {
        var value = GetMemberValue(source, names);

        return value is null ? 0d : Convert.ToDouble(value);
    }

    private static bool TryGetNumericMember(object? source, out double number, params string[] names)
    {
        number = 0d;
        var value = GetMemberValue(source, names);

        if (value is null)
        {
            return false;
        }

        try
        {
            number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetStringMember(object? source, params string[] names)
    {
        return GetMemberValue(source, names)?.ToString();
    }

    private static object? GetMemberValue(object? source, params string[] names)
    {
        if (source is null)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (TryGetDictionaryValue(source, name, out var value))
            {
                return value;
            }
        }

        if (source is IDictionary dictionary)
        {
            foreach (var name in names)
            {
                if (TryGetDictionaryValue(dictionary, name, out var value))
                {
                    return value;
                }
            }
        }

        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var name in names)
        {
            var property = type.GetProperty(name, flags);

            if (property is not null)
            {
                return property.GetValue(source);
            }

            var field = type.GetField(name, flags);

            if (field is not null)
            {
                return field.GetValue(source);
            }
        }

        var extensionData = type.GetProperty("ExtensionData", flags)?.GetValue(source)
            ?? type.GetField("_extensionData", flags)?.GetValue(source);

        if (extensionData is not null)
        {
            foreach (var name in names)
            {
                if (TryGetDictionaryValue(extensionData, name, out var value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static object? FindNestedMemberValue(object? source, string[] containerNames, string[] valueNames)
    {
        if (source is null)
        {
            return null;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return FindNestedMemberValue(source, containerNames, valueNames, visited, 0);
    }

    private static object? FindNestedMemberValue(object source, string[] containerNames, string[] valueNames, HashSet<object> visited, int depth)
    {
        if (depth > 10 || IsSimpleValue(source) || !visited.Add(source))
        {
            return null;
        }

        var container = GetMemberValue(source, containerNames);

        if (container is not null)
        {
            var value = GetMemberValue(container, valueNames);

            if (value is not null)
            {
                return value;
            }
        }

        foreach (var child in EnumerateChildObjects(source))
        {
            var value = FindNestedMemberValue(child, containerNames, valueNames, visited, depth + 1);

            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static IEnumerable<object> EnumerateChildObjects(object source)
    {
        if (source is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is not null && !IsSimpleValue(entry.Value))
                {
                    yield return entry.Value;
                }
            }
        }

        if (source is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item is not null && !IsSimpleValue(item))
                {
                    yield return item;
                }
            }
        }

        var type = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value;

            try
            {
                value = property.GetValue(source);
            }
            catch
            {
                continue;
            }

            if (value is not null && !IsSimpleValue(value))
            {
                yield return value;
            }
        }

        foreach (var field in type.GetFields(flags))
        {
            object? value;

            try
            {
                value = field.GetValue(source);
            }
            catch
            {
                continue;
            }

            if (value is not null && !IsSimpleValue(value))
            {
                yield return value;
            }
        }
    }

    private static bool IsSimpleValue(object value)
    {
        var type = value.GetType();

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(Guid);
    }
}

public class BirdeyeArc186GeneratedBotPatch : AbstractPatch
{
    private const string Arc186Tpl = "6a0b24653f221925eea85485";
    private const string IntegratedAvionicsPartTwoQuestId = "6a0b24653f221925eea854b0";
    private const string BirdeyeBotType = "followerbirdeye";
    private const string BirdeyeComm3BackpackTpl = "628bc7fb408e2b2e9c0801b1";
    public static ProfileHelper? ProfileHelper { get; set; }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotController).GetMethod("TryGenerateSingleBot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(BotController), "TryGenerateSingleBot");
    }

    [PatchPostfix]
    public static void Postfix(MongoId sessionId, ref BotBase __result)
    {
        if (__result is null || !string.Equals(__result.Info?.Settings?.Role, BirdeyeBotType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (IsIntegratedAvionicsPartTwoComplete(sessionId))
        {
            return;
        }

        var inventory = __result.Inventory;
        var items = inventory?.Items;

        if (items is null || items.Any(item => item.Template.ToString() == Arc186Tpl))
        {
            return;
        }

        var backpack = items.FirstOrDefault(item => item.Template.ToString() == BirdeyeComm3BackpackTpl);

        if (backpack is null)
        {
            return;
        }

        items.RemoveAll(item => item.ParentId == backpack.Id.ToString());
        items.Add(new Item
        {
            Id = CreateMongoId(),
            Template = Arc186Tpl,
            ParentId = backpack.Id.ToString(),
            SlotId = "main",
            Location = new ItemLocation
            {
                X = 0,
                Y = 0,
                Rotation = false,
                IsSearched = false
            },
            Upd = new Upd
            {
                SpawnedInSession = true
            }
        });
    }

    private static bool IsIntegratedAvionicsPartTwoComplete(MongoId sessionId)
    {
        try
        {
            var pmcProfile = ProfileHelper?.GetPmcProfile(sessionId);
            var quests = pmcProfile?.Quests;

            if (quests is null)
            {
                return false;
            }

            foreach (var quest in quests)
            {
                if (quest.QId.ToString() == IntegratedAvionicsPartTwoQuestId)
                {
                    return (int)quest.Status >= 4;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static MongoId CreateMongoId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}


