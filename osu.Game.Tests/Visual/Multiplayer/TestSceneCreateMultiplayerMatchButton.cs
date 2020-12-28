// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Screens.OnlinePlay;
using osu.Game.Screens.OnlinePlay.Multiplayer;

namespace osu.Game.Tests.Visual.Multiplayer
{
    public class TestSceneCreateMultiplayerMatchButton : MultiplayerTestScene
    {
        [Cached]
        private OngoingOperationTracker joiningRoomTracker = new OngoingOperationTracker();

        private CreateMultiplayerMatchButton button;

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("create button", () => Child = button = new CreateMultiplayerMatchButton
            {
                Width = 200,
                Height = 100,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        [Test]
        public void TestButtonEnableStateChanges()
        {
            assertButtonEnableState(true);

            AddStep("begin joining room", () => joiningRoomTracker.BeginOperation());
            assertButtonEnableState(false);

            AddStep("end joining room", () => joiningRoomTracker.EndOperation());
            assertButtonEnableState(true);

            AddStep("disconnect client", () => Client.Disconnect());
            assertButtonEnableState(false);

            AddStep("re-connect client", () => Client.Connect());
            assertButtonEnableState(true);
        }

        private void assertButtonEnableState(bool enabled)
            => AddAssert($"button {(enabled ? "enabled" : "disabled")}", () => button.Enabled.Value == enabled);
    }
}
