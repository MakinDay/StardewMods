﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pathoschild.LookupAnything.Framework;
using Pathoschild.LookupAnything.Framework.Subjects;
using Pathoschild.LookupAnything.Framework.Targets;
using StardewValley;

namespace Pathoschild.LookupAnything.Components
{
    /// <summary>Draws debug information to the screen.</summary>
    internal class DebugInterface
    {
        /*********
        ** Properties
        *********/
        /// <summary>Finds and analyses lookup targets in the world.</summary>
        private readonly TargetFactory TargetFactory;

        /// <summary>The warning text to display when debug mode is enabled.</summary>
        public readonly string WarningText;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the debug interface is enabled.</summary>
        public bool Enabled { get; set; }

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="targetFactory">Finds and analyses lookup targets in the world.</param>
        /// <param name="config">The mod configuration.</param>
        public DebugInterface(TargetFactory targetFactory, ModConfig config)
        {
            // save factory
            this.TargetFactory = targetFactory;

            // generate warning text
            string[] keys = new[] { config.Keyboard.ToggleDebug != Keys.None ? config.Keyboard.ToggleDebug.ToString() : null, config.Controller.ToggleDebug?.ToString() }
                    .Where(p => p != null)
                    .ToArray();
            this.WarningText = $"Debug info enabled; press {string.Join(" or ", keys)} to disable.";
        }

        /// <summary>Draw debug metadata to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!this.Enabled)
                return;

            // get location info
            GameLocation currentLocation = Game1.currentLocation;
            Vector2 cursorTile = Game1.currentCursorTile;
            Vector2 cursorPosition = GameHelper.GetScreenCoordinatesFromCursor();

            // show 'debug enabled' warning + cursor position
            {
                string metadata = $"{this.WarningText} Cursor tile ({cursorTile.X}, {cursorTile.Y}), position ({cursorPosition.X}, {cursorPosition.Y}).";
                GameHelper.DrawHoverBox(spriteBatch, metadata, Vector2.Zero, Game1.viewport.Width);
            }

            // show cursor pixel
            spriteBatch.DrawLine(cursorPosition.X - 1, cursorPosition.Y - 1, new Vector2(Game1.pixelZoom, Game1.pixelZoom), Color.DarkRed);

            // show targets within detection radius
            Rectangle tileArea = GameHelper.GetScreenCoordinatesFromTile(Game1.currentCursorTile);
            IEnumerable<ITarget> targets = this.TargetFactory
                .GetNearbyTargets(currentLocation, cursorTile)
                .OrderBy(p => p.Type == TargetType.Unknown ? 0 : 1); // if targets overlap, prioritise info on known targets
            foreach (ITarget target in targets)
            {
                // get metadata
                bool spriteAreaIntersects = target.GetSpriteArea().Intersects(tileArea);
                ISubject subject = this.TargetFactory.GetSubjectFrom(target);

                // draw tile
                {
                    Rectangle tile = GameHelper.GetScreenCoordinatesFromTile(target.GetTile());
                    Color color = (subject != null ? Color.Green : Color.Red) * .5f;
                    spriteBatch.DrawLine(tile.X, tile.Y, new Vector2(tile.Width, tile.Height), color);
                }

                // draw sprite box
                if (subject != null)
                {
                    int borderSize = 3;
                    Color borderColor = Color.Green;
                    if (!spriteAreaIntersects)
                    {
                        borderSize = 1;
                        borderColor *= 0.5f;
                    }

                    Rectangle spriteBox = target.GetSpriteArea();
                    spriteBatch.DrawLine(spriteBox.X, spriteBox.Y, new Vector2(spriteBox.Width, borderSize), borderColor); // top
                    spriteBatch.DrawLine(spriteBox.X, spriteBox.Y, new Vector2(borderSize, spriteBox.Height), borderColor); // left
                    spriteBatch.DrawLine(spriteBox.X + spriteBox.Width, spriteBox.Y, new Vector2(borderSize, spriteBox.Height), borderColor); // right
                    spriteBatch.DrawLine(spriteBox.X, spriteBox.Y + spriteBox.Height, new Vector2(spriteBox.Width, borderSize), borderColor); // bottom
                }
            }

            // show current target name (if any)
            {
                ISubject subject = this.TargetFactory.GetSubjectFrom(currentLocation, cursorTile, cursorPosition);
                if (subject != null)
                    GameHelper.DrawHoverBox(spriteBatch, subject.Name, new Vector2(Game1.getMouseX(), Game1.getMouseY()) + new Vector2(Game1.tileSize / 2f), Game1.viewport.Width / 4f);
            }
        }
    }
}
