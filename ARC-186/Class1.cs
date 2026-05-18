using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Mod;
using System.Collections.Generic;
using System.Threading.Tasks;
using Range = SemanticVersioning.Range;

namespace ARC186;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.arc186.customitem";
    public override string Name { get; init; } = "ARC-186";
    public override string Author { get; init; } = "SpyPilot";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.13");
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";

    public override List<string>? Contributors { get; init; } = new();
    public override List<string>? Incompatibilities { get; init; } = new();
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new();
    public override string? Url { get; init; } = "";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class Arc186ItemService(CustomItemService customItemService) : IOnLoad
{
    public Task OnLoad()
    {
        var arc186 = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5c052f6886f7746b1e3db148",
            ParentId = "57864a66245977548f04a81f",
            NewId = "6a0b24653f221925eea85485",

            OverrideProperties = new TemplateItemProperties
            {
                Name = "ARC-186",
                ShortName = "ARC-186",
                Description = "Airborne SINCGARS will provide jam-resistant, secure VHF/FM/AM communications in the cockpit. This enables interoperable communications with the Army's SINCGARS systems during close air support missions.",

                Width = 2,
                Height = 1,
                Weight = 0.25,
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

        return Task.CompletedTask;
    }
}