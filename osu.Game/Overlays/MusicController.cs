﻿//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Game.Beatmaps.IO;
using osu.Game.Database;
using osu.Game.Graphics;

namespace osu.Game.Overlays
{
    public class MusicController : OverlayContainer
    {
        private Sprite backgroundSprite;
        private Box progress;
        private TextAwesome playButton, listButton;
        private SpriteText title, artist;
        private OsuGameBase osuGame;
        private List<BeatmapSetInfo> playList;
        private BeatmapSetInfo currentPlay;
        public AudioTrack CurrentTrack { get; set; }//TODO:gets exterally
        public override void Load(BaseGame game)
        {
            base.Load(game);
            osuGame = game as OsuGameBase;
            playList = osuGame.Beatmaps.Query<BeatmapSetInfo>().ToList();
            currentPlay = playList.FirstOrDefault();
            Width = 400;
            Height = 130;
            CornerRadius = 5;
            Masking = true;

            Children = new Drawable[]
            {
                backgroundSprite = getScaledSprite(game.Textures.Get(@"Backgrounds/bg4")),//placeholder
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0, 0, 0, 127)
                },
                title = new SpriteText
                {
                    Origin = Anchor.BottomCentre,
                    Anchor = Anchor.TopCentre,
                    Position = new Vector2(0, 40),
                    TextSize = 20,
                    Colour = Color4.White,
                    Text = @"Nothing to play"
                },
                artist = new SpriteText
                {
                    Origin = Anchor.TopCentre,
                    Anchor = Anchor.TopCentre,
                    Position = new Vector2(0, 45),
                    TextSize = 12,
                    Colour = Color4.White,
                    Text = @"Nothing to play"
                },
                new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 50,
                    Origin = Anchor.BottomCentre,
                    Anchor = Anchor.BottomCentre,
                    Colour = new Color4(0, 0, 0, 127)
                },
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.BottomCentre,
                    Position = new Vector2(0, 30),
                    Action = () =>
                    {
                        if (CurrentTrack == null) return;
                        if (CurrentTrack.IsRunning)
                        {
                            CurrentTrack.Stop();
                            playButton.Icon = FontAwesome.play_circle_o;
                        }
                        else
                        {
                            CurrentTrack.Start();
                            playButton.Icon = FontAwesome.pause;
                        }
                    },
                    Children = new Drawable[]
                    {
                        playButton = new TextAwesome
                        {
                            TextSize = 30,
                            Icon = FontAwesome.play_circle_o,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        }
                    }
                },
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.BottomCentre,
                    Position = new Vector2(-30, 30),
                    Action = prev,
                    Children = new Drawable[]
                    {
                        new TextAwesome
                        {
                            TextSize = 15,
                            Icon = FontAwesome.step_backward,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        }
                    }
                },
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.BottomCentre,
                    Position = new Vector2(30, 30),
                    Action = next,
                    Children = new Drawable[]
                    {
                        new TextAwesome
                        {
                            TextSize = 15,
                            Icon = FontAwesome.step_forward,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        }
                    }
                },
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.BottomRight,
                    Position = new Vector2(20, 30),
                    Children = new Drawable[]
                    {
                        listButton = new TextAwesome
                        {
                            TextSize = 15,
                            Icon = FontAwesome.bars,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        }
                    }
                },
                progress = new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 10,
                    Width = 0,
                    Origin = Anchor.BottomLeft,
                    Anchor = Anchor.BottomLeft,
                    Colour = Color4.Orange
                }
            };
            if (currentPlay != null)
            {
                playButton.Icon=FontAwesome.pause;
                play(currentPlay, null);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (CurrentTrack == null) return;
            progress.Width = (float)(CurrentTrack.CurrentTime / CurrentTrack.Length);
            if (CurrentTrack.HasCompleted) next();
        }

        private void prev()
        {
            int i = playList.IndexOf(currentPlay);
            if (i == -1) return;
            i = (i - 1 + playList.Count) % playList.Count;
            currentPlay = playList[i];
            play(currentPlay, false);
        }

        private void next()
        {
            int i = playList.IndexOf(currentPlay);
            if (i == -1) return;
            i = (i + 1) % playList.Count;
            currentPlay = playList[i];
            play(currentPlay, true);
        }

        private void play(BeatmapSetInfo beatmap, bool? isNext)
        {
            BeatmapMetadata metadata = osuGame.Beatmaps.Query<BeatmapMetadata>().Where(x => x.ID == beatmap.BeatmapMetadataID).First();
            title.Text = metadata.TitleUnicode ?? metadata.Title;
            artist.Text = metadata.ArtistUnicode ?? metadata.Artist;
            Sprite newBackground;
            using (ArchiveReader reader = osuGame.Beatmaps.GetReader(currentPlay))
            {
                CurrentTrack?.Stop();
                CurrentTrack = new AudioTrackBass(reader.ReadFile(metadata.AudioFile));
                CurrentTrack.Start();
                newBackground = getScaledSprite(TextureLoader.FromStream(reader.ReadFile(metadata.BackgroundFile)));
            }
            Add(newBackground);
            if (isNext == true)
            {
                newBackground.Position = new Vector2(400, 0);
                newBackground.MoveToX(0, 500, EasingTypes.OutCubic);
                backgroundSprite.MoveToX(-400, 500, EasingTypes.OutCubic);
                backgroundSprite.Expire();
            }
            else if (isNext == false)
            {
                newBackground.Position = new Vector2(-400, 0);
                newBackground.MoveToX(0, 500, EasingTypes.OutCubic);
                backgroundSprite.MoveToX(400, 500, EasingTypes.OutCubic);
                backgroundSprite.Expire();
            }
            else
            {
                Remove(backgroundSprite);
            }
            backgroundSprite = newBackground;
        }

        private Sprite getScaledSprite(Texture background)
        {
            Sprite scaledSprite = new Sprite
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Texture = background,
                Depth = float.MinValue
            };
            scaledSprite.Scale = new Vector2(Math.Max(DrawSize.X / scaledSprite.DrawSize.X, DrawSize.Y / scaledSprite.DrawSize.Y));
            return scaledSprite;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            trySeek(state);
            return base.OnMouseDown(state, args);
        }

        protected override bool OnMouseMove(InputState state)
        {
            trySeek(state);
            return base.OnMouseMove(state);
        }

        private void trySeek(InputState state)
        {
            if (state.Mouse.LeftButton)
            {
                Vector2 pos = GetLocalPosition(state.Mouse.NativeState.Position);
                if (pos.Y > 120)
                    CurrentTrack?.Seek(CurrentTrack.Length * pos.X / 400f);
            }
        }

        //placeholder for toggling
        protected override void PopIn() => FadeIn(500);

        protected override void PopOut() => FadeOut(500);
    }
}
