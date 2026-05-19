using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Range = SemanticVersioning.Range;

namespace ARC186;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.arc186.customitem";
    public override string Name { get; init; } = "ARC-186";
    public override string Author { get; init; } = "SpyPilot";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.2");
    public override Range SptVersion { get; init; } = new("~4.0.13");
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";

    public override List<string>? Contributors { get; init; } = new();
    public override List<string>? Incompatibilities { get; init; } = new();
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new();
    public override string? Url { get; init; } = "https://github.com/SpyPilotEFT/ARC-186";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class Arc186ItemService(
    ISptLogger<Arc186ItemService> logger,
    CustomItemService customItemService,
    DatabaseService databaseService) : IOnLoad
{
    private const string Arc186Tpl = "6a0b24653f221925eea85485";

    public Task OnLoad()
    {
        var arc186 = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5c052f6886f7746b1e3db148",
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
        AddArc186ToHallOfFameLargeTrophies();

        logger.Info("ARC-186: Hall of Fame large trophy support enabled.");

        return Task.CompletedTask;
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

    private static IEnumerable<object> EnumerateDatabaseValues(object databaseItems)
    {
        foreach (var entry in (IEnumerable)databaseItems)
        {
            var valueProperty = entry.GetType().GetProperty("Value");

            yield return valueProperty?.GetValue(entry) ?? entry;
        }
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

    private static void AddValueToCollection(object collection, string value)
    {
        var collectionType = collection.GetType();
        var elementType = GetCollectionElementType(collectionType);
        var typedValue = CreateValue(elementType, value);

        var addMethod = collectionType.GetMethod("Add", [elementType]);

        addMethod?.Invoke(collection, [typedValue]);
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

        var stringConstructor = targetType.GetConstructor([typeof(string)]);

        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke([value]);
        }

        return value;
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

        var type = source.GetType();

        foreach (var name in names)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            if (property is not null)
            {
                return property.GetValue(source);
            }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            if (field is not null)
            {
                return field.GetValue(source);
            }
        }

        return null;
    }
}