// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModUnlimitationTest
    {
        [Test]
        public void TestRegisteredAsConversionMod()
        {
            Assert.That(new OsuRuleset().GetModsFor(ModType.Conversion).OfType<OsuModUnlimitation>(), Has.Exactly(1).Items);
        }

        [Test]
        public void TestIncompatibleDifficultyMods()
        {
            var unlimitation = new OsuModUnlimitation();

            Assert.Multiple(() =>
            {
                Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { unlimitation, new OsuModDifficultyAdjust() }), Is.False);
                Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { unlimitation, new OsuModEasy() }), Is.False);
                Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { unlimitation, new OsuModHardRock() }), Is.False);
                Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { unlimitation, new OsuModTargetPractice() }), Is.False);
            });
        }

        [Test]
        public void TestApplyToDifficultyCustomValues()
        {
            var mod = new OsuModUnlimitation
            {
                CircleSize = { Value = -123.4f },
                ApproachRate = { Value = 9999.9f },
                DrainRate = { Value = -500.2f },
                OverallDifficulty = { Value = 10000f }
            };

            var difficulty = new BeatmapDifficulty();
            mod.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(-123.4f).Within(1e-3f));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(9999.9f).Within(1e-3f));
            Assert.That(difficulty.DrainRate, Is.EqualTo(-500.2f).Within(1e-3f));
            Assert.That(difficulty.OverallDifficulty, Is.EqualTo(10000f).Within(1e-3f));
        }

        [Test]
        public void TestUnsetValuesPreserveDifficulty()
        {
            var difficulty = new BeatmapDifficulty
            {
                CircleSize = 4,
                ApproachRate = 8,
                DrainRate = 6,
                OverallDifficulty = 9,
            };

            new OsuModUnlimitation().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(4));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(8));
            Assert.That(difficulty.DrainRate, Is.EqualTo(6));
            Assert.That(difficulty.OverallDifficulty, Is.EqualTo(9));
        }

        [Test]
        public void TestValuesAreClampedToSupportedRange()
        {
            var mod = new OsuModUnlimitation
            {
                CircleSize = { Value = 100 },
                ApproachRate = { Value = -20000 },
                DrainRate = { Value = 20000 },
                OverallDifficulty = { Value = -20000 },
            };

            Assert.That(mod.CircleSize.Value, Is.EqualTo(12));
            Assert.That(mod.ApproachRate.Value, Is.EqualTo(-10000));
            Assert.That(mod.DrainRate.Value, Is.EqualTo(10000));
            Assert.That(mod.OverallDifficulty.Value, Is.EqualTo(-10000));
        }
    }
}
