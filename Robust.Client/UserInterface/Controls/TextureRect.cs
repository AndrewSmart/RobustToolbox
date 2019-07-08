﻿using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Simple control that draws a single texture using a variety of possible stretching modes.
    /// </summary>
    [ControlWrap("TextureRect")]
    public class TextureRect : Control
    {
        private bool _canShrink;
        private Texture _texture;

        public TextureRect()
        {
        }

        public TextureRect(string name) : base(name)
        {
        }

        /// <summary>
        ///     The texture to draw.
        /// </summary>
        public Texture Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                MinimumSizeChanged();
            }
        }

        /// <summary>
        ///     If true, this control can shrink below the size of <see cref="Texture"/>.
        /// </summary>
        /// <remarks>
        ///     This does not set <see cref="Control.RectClipContent"/>.
        ///     Certain stretch modes may display outside the area of the control unless it is set.
        /// </remarks>
        public bool CanShrink
        {
            get => _canShrink;
            set
            {
                _canShrink = value;
                MinimumSizeChanged();
            }
        }

        /// <summary>
        ///     Controls how the texture should be drawn if the control is larger than the size of the texture.
        /// </summary>
        public StretchMode Stretch { get; set; } = StretchMode.Keep;

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_texture == null)
            {
                return;
            }

            switch (Stretch)
            {
                case StretchMode.Scale:
                    handle.DrawTexture(_texture, Vector2.Zero);
                    break;
                case StretchMode.Tile:
                // TODO: Implement Tile.
                case StretchMode.Keep:
                    handle.DrawTextureRectRegion(_texture, UIBox2.FromDimensions(Vector2.Zero, _texture.Size));
                    break;
                case StretchMode.KeepCentered:
                {
                    var position = (PixelSize - _texture.Size) / 2;
                    handle.DrawTexture(_texture, position);
                    break;
                }

                case StretchMode.KeepAspect:
                case StretchMode.KeepAspectCentered:
                {
                    var width = _texture.Width * (Size.Y / _texture.Height);
                    var height = Size.Y;
                    if (width > Size.X)
                    {
                        width = Size.X;
                        height = _texture.Height * (Size.X / _texture.Width);
                    }

                    var size = new Vector2(width, height);
                    var position = Vector2.Zero;
                    if (Stretch == StretchMode.KeepAspectCentered)
                    {
                        position = (Size - size) / 2;
                    }

                    handle.DrawTextureRectRegion(_texture, UIBox2.FromDimensions(position, size));
                    break;
                }

                case StretchMode.KeepAspectCovered:
                    // Calculate the scale necessary to fit width and height to control size.
                    var (scaleX, scaleY) = Size / _texture.Size;
                    // Use whichever scale is greater.
                    var scale = Math.Max(scaleX, scaleY);
                    // Offset inside the actual texture.
                    var offset = (_texture.Size - Size) / scale / 2f;
                    handle.DrawTextureRectRegion(_texture, SizeBox, UIBox2.FromDimensions(offset, Size / scale));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "texture")
            {
                var extRef = context.GetExtResource((GodotAsset.TokenExtResource) value);
                ResourcePath godotPathToResourcePath;
                try
                {
                    godotPathToResourcePath = GodotPathUtility.GodotPathToResourcePath(extRef.Path);
                }
                catch (ArgumentException)
                {
                    Logger.Error("TextureRect is referencing non-VFS Godot path {0}.", extRef.Path);
                    return;
                }

                var texture = IoCManager.Resolve<IResourceCache>()
                    .GetResource<TextureResource>(godotPathToResourcePath);
                Texture = texture;
            }
        }

        public enum StretchMode
        {
            /// <summary>
            ///     The texture is stretched to fit the entire area of the control.
            /// </summary>
            Scale = 1,

            /// <summary>
            ///     The texture is tiled to fit the entire area of the control, without stretching.
            /// </summary>
            Tile = 2,

            /// <summary>
            ///     The texture is drawn in its correct size, in the top left corner of the control.
            /// </summary>
            Keep = 3,

            /// <summary>
            ///     The texture is drawn in its correct size, in the center of the control.
            /// </summary>
            KeepCentered = 4,

            /// <summary>
            ///     The texture is stretched to take as much space as possible,
            ///     while maintaining the original aspect ratio.
            ///     The texture is positioned from the top left corner of the control.
            ///     The texture remains completely visible, potentially leaving some sections of the control blank.
            /// </summary>
            KeepAspect = 5,

            /// <summary>
            ///     <see cref="KeepAspect"/>, but the texture is centered instead.
            /// </summary>
            KeepAspectCentered = 7,

            /// <summary>
            ///     <see cref="KeepAspectCentered"/>, but the texture covers the entire control,
            ///     potentially cutting out part of the texture.
            /// </summary>
            /// <example>
            ///     This effectively causes the entire control to be filled with the texture,
            ///     while preserving aspect ratio.
            /// </example>
            KeepAspectCovered = 8
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (_texture == null || CanShrink)
            {
                return Vector2.Zero;
            }

            return Texture.Size;
        }
    }
}
