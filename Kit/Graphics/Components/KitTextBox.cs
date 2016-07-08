﻿using System.Collections.Generic;
using System.Windows.Input;
using Kit.Core;
using Kit.Graphics.Drawing;
using Kit.Graphics.Types;

namespace Kit.Graphics.Components
{
    class KitTextBox : KitComponent
    {
        private double lastFlashTime;
        private bool dashOn;

        public KitText TextField { get; set; }

        private TextIOFormatter formatter;

        public KitTextBox(double fontSize, double maxWidth, Vector2 location = default(Vector2))
            : base(location)
        {
            Anchor = KitAnchoring.LeftCenter;
            TextField = new KitText("TextField", "Consolas", fontSize, Vector2.Zero, Size)
            {
                Origin = KitAnchoring.LeftCenter,
                TextColor = System.Windows.Media.Colors.Black,
                ShouldDraw = false
            };
            formatter = new TextIOFormatter(TextField);

            AddChild(TextField);
            Masked = true;
            Vector2 TextMetrics = KitBrush.GetTextBounds("|", TextField.Font);
            Size = new Vector2(maxWidth + 4, TextMetrics.Y + 4);
            lastFlashTime = time;
        }

        public override void PreDrawComponent(KitBrush brush)
        {
            foreach (KitComponent child in Children)
            {
                child.ComponentDepth = ComponentDepth + 0.01;
            }
            base.PreDrawComponent(brush);
        }

        protected override void OnUpdate()
        {
            if (!Focused && !TextField.Focused)
            {
                lastFlashTime = -1;
            }
            else
            {
                if (lastFlashTime == -1)
                {
                    dashOn = true;
                    lastFlashTime = time;
                    Redraw = true;
                }

                if (time - lastFlashTime > 600)
                {
                    lastFlashTime = time;
                    dashOn = !dashOn;
                    Redraw = true;
                }
            }
            base.OnUpdate();
        }

        protected override void OnTextInput(string text)
        {
            if ((Focused || TextField.Focused)
                && text.Length > 0 && text[0] >= ' ')
            {
                formatter.InsertText(text);
                lastFlashTime = time;
                dashOn = true;
                Redraw = true;
            }
            base.OnTextInput(text);
        }

        protected override void OnKeyInput(Key key, KeyState state)
        {
            if (Focused || TextField.Focused)
            {
                formatter.OnKey(key, state);
                if (state == KeyState.Press || state == KeyState.Hold)
                {
                    if (formatter.HandleKeyPress(key))
                    {
                        lastFlashTime = time;
                        dashOn = true;
                        Redraw = true;
                    }
                }

            }
            base.OnKeyInput(key, state);
        }

        protected override void OnMouseInput(Vector2 clickLocation, MouseState mouseFlags)
        {
            if (Focused || TextField.Focused)
            {
                Vector2 relativeClick = clickLocation - TextField.GetAbsoluteLocation();
                if (relativeClick.X <= 0)
                {
                    formatter.CursorLoc = 0;
                }
                else if (relativeClick.X >= TextField.Size.X)
                {
                    formatter.CursorLoc = formatter.CURSOR_END;
                }
                else
                {
                    int i = 0;
                    for (; i < TextField.Text.Length; i++)
                    {
                        Vector2 textDims = KitBrush.GetTextBounds(TextField.Text.Substring(0, i), TextField.Font);
                        Vector2 nextTextDims = KitBrush.GetTextBounds(TextField.Text.Substring(0, i + 1), TextField.Font);
                        if (relativeClick.X >= textDims.X && relativeClick.X <= nextTextDims.X)
                        {
                            if (relativeClick.X - textDims.X > nextTextDims.X - relativeClick.X)
                            {
                                i++;
                            }
                            break;
                        }
                    }
                    if(i == TextField.Text.Length)
                    {
                        i = formatter.CURSOR_END;
                    }
                    formatter.CursorLoc = i;
                }
                dashOn = true;
                Redraw = true;
                lastFlashTime = time;
                formatter.EndHighlight();
            }
            base.OnMouseInput(clickLocation, mouseFlags);
        }

        protected override void DrawComponent(KitBrush brush)
        {
            Vector2 lineStart = GetAbsoluteLocation();
            Vector2 lineEnd = new Vector2(lineStart.X, lineStart.Y + Size.Y);
            System.Windows.Media.Color nc = System.Windows.Media.Color.FromArgb(0x7F, 0xFF, 0, 0);
            brush.DrawRectangle(new Box(new Vector2(lineStart.X, lineStart.Y), new Vector2(Size.X, Size.Y)), true, nc);

            double pixelCursorOffset = formatter.GetCursorOffset();

            lineStart.X += pixelCursorOffset;
            lineEnd.X += pixelCursorOffset;
            lineStart.Y += 1;
            lineEnd.Y -= 1;

            pushNecessaryClips(brush);

            if(formatter.Highlighting())
            {
                System.Windows.Media.Color hColor = System.Windows.Media.Color.FromArgb(0x7F, 0, 0, 0xFF);

                Box highlightRect = formatter.GetHighlightRect();

                highlightRect.Pos += TextField.GetAbsoluteLocation();

                brush.DrawRectangle(highlightRect, true, hColor);
            }

            if (dashOn && (Focused || TextField.Focused))
            {
                lineStart.X = System.Math.Round(lineStart.X) + 0.5;
                lineEnd.X = System.Math.Round(lineEnd.X) + 0.5;
                brush.DrawLine(lineStart, lineEnd, TextField.TextColor, 1);
                brush.DrawLine(lineStart, lineEnd, TextField.TextColor, 1);
            }
            popNecessaryClips(brush);

            TextField._DrawComponent(brush);
            base.DrawComponent(brush);
        }
    }
}