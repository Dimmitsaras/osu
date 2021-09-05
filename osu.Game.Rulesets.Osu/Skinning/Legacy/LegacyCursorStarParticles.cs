// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Bindings;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Particles;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Play;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Osu.Skinning.Legacy
{
    public class LegacyCursorStarParticles : BeatSyncedContainer, IKeyBindingHandler<OsuAction>
    {
        private StarParticleSpewer breakSpewer;
        private StarParticleSpewer kiaiSpewer;

        [Resolved(canBeNull: true)]
        private Player player { get; set; }

        [Resolved(canBeNull: true)]
        private OsuPlayfield osuPlayfield { get; set; }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, OsuColour colour)
        {
            var texture = skin.GetTexture("star2");

            InternalChildren = new[]
            {
                breakSpewer = new StarParticleSpewer(texture, 20)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = colour.PinkLighter,
                    Direction = SpewDirection.None,
                    Active =
                    {
                        Value = false,
                    }
                },
                kiaiSpewer = new StarParticleSpewer(texture, 60)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = colour.PinkLighter,
                    Direction = SpewDirection.None,
                    Active =
                    {
                        Value = false,
                    }
                },
            };

            if (player != null)
            {
                breakSpewer.Active.BindTarget = player.IsBreakTime;
            }

            if (osuPlayfield != null)
            {
                breakSpewer.ParticleParent = osuPlayfield;
                kiaiSpewer.ParticleParent = osuPlayfield;
            }
        }

        protected override void Update()
        {
            if (osuPlayfield == null) return;

            // find active kiai slider or spinner.
            var kiaiHitObject = osuPlayfield.HitObjectContainer.AliveObjects.FirstOrDefault(h =>
                h.HitObject.Kiai &&
                (
                    (h is DrawableSlider slider && slider.Tracking.Value) ||
                    (h is DrawableSpinner spinner && spinner.RotationTracker.Tracking)
                )
            );

            kiaiSpewer.Active.Value = kiaiHitObject != null;
        }

        public bool OnPressed(OsuAction action)
        {
            handleInput(action, true);
            return false;
        }

        public void OnReleased(OsuAction action)
        {
            handleInput(action, false);
        }

        private bool leftPressed;
        private bool rightPressed;

        private void handleInput(OsuAction action, bool pressed)
        {
            switch (action)
            {
                case OsuAction.LeftButton:
                    leftPressed = pressed;
                    break;

                case OsuAction.RightButton:
                    rightPressed = pressed;
                    break;
            }

            if (leftPressed && rightPressed)
                breakSpewer.Direction = SpewDirection.Both;
            else if (leftPressed)
                breakSpewer.Direction = SpewDirection.Left;
            else if (rightPressed)
                breakSpewer.Direction = SpewDirection.Right;
            else
                breakSpewer.Direction = SpewDirection.None;
        }

        private class StarParticleSpewer : ParticleSpewer
        {
            private const int particle_lifetime_min = 300;
            private const int particle_lifetime_max = 1000;

            public SpewDirection Direction { get; set; }

            protected override float ParticleGravity => 240;

            public StarParticleSpewer(Texture texture, int perSecond)
                : base(texture, perSecond, particle_lifetime_max)
            {
                Active.BindValueChanged(_ => resetVelocityCalculation());
            }

            private Vector2 positionInParent => ToSpaceOfOtherDrawable(OriginPosition, ParticleParent);

            private Vector2 screenVelocity;

            private const double velocity_calculation_delay = 15;
            private double lastVelocityCalculation;
            private Vector2 positionDifference;
            private Vector2? lastPosition;

            protected override void Update()
            {
                base.Update();

                if (lastPosition != null)
                {
                    positionDifference += (positionInParent - lastPosition.Value);
                    lastVelocityCalculation += Clock.ElapsedFrameTime;
                }

                lastPosition = positionInParent;

                if (lastVelocityCalculation > velocity_calculation_delay)
                {
                    screenVelocity = positionDifference / (float)lastVelocityCalculation;

                    positionDifference = Vector2.Zero;
                    lastVelocityCalculation = 0;
                }
            }

            private void resetVelocityCalculation()
            {
                positionDifference = Vector2.Zero;
                lastVelocityCalculation = 0;
                lastPosition = null;
            }

            protected override FallingParticle SpawnParticle()
            {
                var p = base.SpawnParticle();

                p.StartPosition = positionInParent;
                p.Duration = RNG.NextSingle(particle_lifetime_min, particle_lifetime_max);
                p.AngularVelocity = RNG.NextSingle(-3f, 3f);
                p.StartScale = RNG.NextSingle(0.5f, 1f);
                p.EndScale = RNG.NextSingle(2f);

                switch (Direction)
                {
                    case SpewDirection.None:
                        p.Velocity = Vector2.Zero;
                        break;

                    case SpewDirection.Left:
                        p.Velocity = new Vector2(
                            RNG.NextSingle(-460f, 0),
                            RNG.NextSingle(-40f, 40f)
                        );
                        break;

                    case SpewDirection.Right:
                        p.Velocity = new Vector2(
                            RNG.NextSingle(0, 460f),
                            RNG.NextSingle(-40f, 40f)
                        );
                        break;

                    case SpewDirection.Both:
                        p.Velocity = new Vector2(
                            RNG.NextSingle(-460f, 460f),
                            RNG.NextSingle(-160f, 160f)
                        );
                        break;
                }

                p.Velocity += screenVelocity * 40;

                return p;
            }
        }

        private enum SpewDirection
        {
            None,
            Left,
            Right,
            Both,
        }
    }
}
