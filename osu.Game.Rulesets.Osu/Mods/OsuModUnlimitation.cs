// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Allows osu! difficulty values to be overridden beyond the limits exposed by
    /// <see cref="OsuModDifficultyAdjust"/>.
    /// </summary>
    /// <remarks>
    /// Adapted from qwerwas/Osu.Mod.Unlimitation, originally created by MrrtyI.
    /// </remarks>
    public partial class OsuModUnlimitation : Mod, IApplicableToDifficulty
    {
        public override string Name => "Unlimitation";

        public override LocalisableString Description => "Override a beatmap's difficulty settings beyond standard limits.";

        public override string Acronym => "UL";

        public override ModType Type => ModType.Conversion;

        public override IconUsage? Icon => OsuIcon.ModDifficultyAdjust;

        public override bool RequiresConfiguration => true;

        public override bool ValidForFreestyleAsRequiredMod => true;

        public override Type[] IncompatibleMods => new[]
        {
            typeof(ModEasy),
            typeof(ModHardRock),
            typeof(ModDifficultyAdjust),
            typeof(OsuModTargetPractice),
        };

        [SettingSource("Circle Size", "Override a beatmap's set CS.", 1, SettingControlType = typeof(UnlimitationSettingsControl))]
        public DifficultyBindable CircleSize { get; } = new DifficultyBindable
        {
            Precision = 0.1f,
            MinValue = -10000,
            MaxValue = 12,
            ReadCurrentFromDifficulty = difficulty => difficulty.CircleSize,
        };

        [SettingSource("Approach Rate", "Override a beatmap's set AR.", 2, SettingControlType = typeof(UnlimitationSettingsControl))]
        public DifficultyBindable ApproachRate { get; } = new DifficultyBindable
        {
            Precision = 0.1f,
            MinValue = -10000,
            MaxValue = 10000,
            ReadCurrentFromDifficulty = difficulty => difficulty.ApproachRate,
        };

        [SettingSource("HP Drain", "Override a beatmap's set HP.", 3, SettingControlType = typeof(UnlimitationSettingsControl))]
        public DifficultyBindable DrainRate { get; } = new DifficultyBindable
        {
            Precision = 0.1f,
            MinValue = -10000,
            MaxValue = 10000,
            ReadCurrentFromDifficulty = difficulty => difficulty.DrainRate,
        };

        [SettingSource("Accuracy", "Override a beatmap's set OD.", 4, SettingControlType = typeof(UnlimitationSettingsControl))]
        public DifficultyBindable OverallDifficulty { get; } = new DifficultyBindable
        {
            Precision = 0.1f,
            MinValue = -10000,
            MaxValue = 10000,
            ReadCurrentFromDifficulty = difficulty => difficulty.OverallDifficulty,
        };

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            if (CircleSize.Value != null)
                difficulty.CircleSize = CircleSize.Value.Value;

            if (ApproachRate.Value != null)
                difficulty.ApproachRate = ApproachRate.Value.Value;

            if (DrainRate.Value != null)
                difficulty.DrainRate = DrainRate.Value.Value;

            if (OverallDifficulty.Value != null)
                difficulty.OverallDifficulty = OverallDifficulty.Value.Value;
        }

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                if (!CircleSize.IsDefault)
                    yield return ("Circle size", $"{CircleSize.Value:N1}");

                if (!ApproachRate.IsDefault)
                    yield return ("Approach rate", $"{ApproachRate.Value:N1}");

                if (!DrainRate.IsDefault)
                    yield return ("HP drain", $"{DrainRate.Value:N1}");

                if (!OverallDifficulty.IsDefault)
                    yield return ("Accuracy", $"{OverallDifficulty.Value:N1}");
            }
        }

        public override string ExtendedIconInformation
        {
            get
            {
                if (!isExactlyOneSettingChanged(CircleSize, ApproachRate, OverallDifficulty, DrainRate))
                    return string.Empty;

                if (!CircleSize.IsDefault) return format("CS", CircleSize);
                if (!ApproachRate.IsDefault) return format("AR", ApproachRate);
                if (!OverallDifficulty.IsDefault) return format("OD", OverallDifficulty);
                if (!DrainRate.IsDefault) return format("HP", DrainRate);

                return string.Empty;

                static string format(string acronym, DifficultyBindable bindable)
                    => $"{acronym}{bindable.Value!.Value.ToStandardFormattedString(1)}";
            }
        }

        private static bool isExactlyOneSettingChanged(params DifficultyBindable[] difficultySettings)
            => difficultySettings.Count(setting => !setting.IsDefault) == 1;

        public partial class UnlimitationSettingsControl : SettingsItem<float?>
        {
            [Resolved]
            private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

            private readonly Bindable<string> displayedValue = new Bindable<string>();
            private DifficultyBindable difficultyBindable = null!;
            private bool isInternalChange;

            protected override Drawable CreateControl() => new TextBoxControl(displayedValue, commitText);

            public override Bindable<float?> Current
            {
                get => base.Current;
                set
                {
                    difficultyBindable = (DifficultyBindable)value.GetBoundCopy();
                    base.Current = difficultyBindable;
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(_ => updateDisplayedText(), true);
                beatmap.BindValueChanged(_ => updateDisplayedText(), true);
            }

            private void updateDisplayedText()
            {
                if (isInternalChange)
                    return;

                try
                {
                    isInternalChange = true;

                    if (Current.Value != null)
                    {
                        displayedValue.Value = Current.Value.Value.ToString("0.0", CultureInfo.CurrentCulture);
                        return;
                    }

                    if (difficultyBindable.ReadCurrentFromDifficulty != null)
                    {
                        displayedValue.Value = difficultyBindable.ReadCurrentFromDifficulty(beatmap.Value.BeatmapInfo.Difficulty)
                                                                   .ToString("0.0", CultureInfo.CurrentCulture);
                    }
                }
                finally
                {
                    isInternalChange = false;
                }
            }

            private void commitText(string newText)
            {
                if (isInternalChange)
                    return;

                if (string.IsNullOrWhiteSpace(newText))
                {
                    Current.Value = null;
                    return;
                }

                if (float.TryParse(newText, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed))
                {
                    Current.Value = parsed;
                    updateDisplayedText();
                    return;
                }

                updateDisplayedText();
            }

            private partial class TextBoxControl : CompositeDrawable, IHasCurrentValue<float?>
            {
                private readonly DifficultyBindableWithCurrent current = new DifficultyBindableWithCurrent();

                public Bindable<float?> Current
                {
                    get => current.Current;
                    set => current.Current = value;
                }

                public TextBoxControl(Bindable<string> displayedValue, Action<string> commitAction)
                {
                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    var numberBox = new FormNumberBox(allowDecimals: true)
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 1,
                        PlaceholderText = "Default",
                        Current = displayedValue,
                    };

                    numberBox.OnCommit += (_, _) => commitAction(displayedValue.Value);
                    InternalChild = numberBox;
                }
            }

            private class DifficultyBindableWithCurrent : DifficultyBindable, IHasCurrentValue<float?>
            {
                private Bindable<float?> currentBound = null!;

                public Bindable<float?> Current
                {
                    get => this;
                    set
                    {
                        ArgumentNullException.ThrowIfNull(value);

                        if (currentBound != null)
                            UnbindFrom(currentBound);

                        BindTo(currentBound = value);
                    }
                }

                protected override Bindable<float?> CreateInstance() => new DifficultyBindableWithCurrent();
            }
        }
    }
}
