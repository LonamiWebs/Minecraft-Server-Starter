/// <copyright file="MOTDTextBox.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>TextBox which allows generating Minecraft MOTDs</summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Minecraft_Server_Starter
{
    public partial class MOTDTextBox : UserControl
    {
        #region Glyphs sizes

        // Characters pixel width info. All these combinations make a full line:
        //     42  characters of any character (such as _-O1)
        //     54  characters of <{("*f)}>
        //     67  characters of [tI] PLUS a period . at the end
        //     68  spaces PLUS a period . at the end
        //     90  characters of `'l
        //     135 characters of ,.!:;i|
        // 
        // Translated into percentage equals to:
        //    42  → 0.02380952380952380952380952380952
        //    54  → 0.01851851851851851851851851851852
        //    67  → 0.0149802428188276723378989824539  ((1 / 67) + ((1 / 135) / 135))
        //    68  → 0.01476075203744049059953199386742
        //    90  → 0.01111111111111111111111111111111
        //    135 → 0,00740740740740740740740740740741
        // 
        // 
        // The following characters need special encoding:
        //     ´¡· and accented letters

        // gets the space that a character occupies, in percentage, in a motd line
        static double getCharacterPixelSize(char c, bool bold)
        {
            // TODO bold factor probably isn't this one, but it does the trick
            double boldFactor = bold ? 1.155555555555555555555555555556 : 1;
            switch (c)
            {
                case ' ':
                    return 0.01476075203744049059953199386742 * boldFactor;

                case ',':
                case '.':
                case '!':
                case ':':
                case ';':
                case 'i':
                case '|':
                    return 0.00740740740740740740740740740741 * boldFactor;

                case '[':
                case 't':
                case 'I':
                case ']':
                    return 0.0149802428188276723378989824539 * boldFactor;


                case '`':
                case '\'':
                case 'l':
                    return 0.01111111111111111111111111111111 * boldFactor;

                case '<':
                case '{':
                case '(':
                case '"':
                case '*':
                case 'f':
                case ')':
                case '}':
                case '>':
                    return 0.01851851851851851851851851851852 * boldFactor;

                default:
                    return 0.02380952380952380952380952380952 * boldFactor;
            }
        }

        #endregion

        #region Constant values

        // represents the percentage value of all the pixels in a motd line, this is, 1
        const double maxPixelLength = 1d;

        // prefixes
        const char chatStylePrefix = '§';
        const string propertiesStylePrefix = @"\u00A7";

        // initial values
        static readonly CharacterStyle initialStyle = 0;
        static readonly Color initialColor = Color.FromRgb(0xAA, 0xAA, 0xAA);

        // colors
        static readonly Dictionary<Color, char> ColorCode = new Dictionary<Color, char>
        {
            { Color.FromRgb(0xAA, 0xAA, 0xAA), '7' }, // color used by default

            { Color.FromRgb(0x00, 0x00, 0x00), '0' },
            { Color.FromRgb(0x00, 0x00, 0xAA), '1' },
            { Color.FromRgb(0x00, 0xAA, 0x00), '2' },
            { Color.FromRgb(0x00, 0xAA, 0xAA), '3' },
            { Color.FromRgb(0xAA, 0x00, 0x00), '4' },
            { Color.FromRgb(0xAA, 0x00, 0xAA), '5' },
            { Color.FromRgb(0xFF, 0xAA, 0x00), '6' },
            { Color.FromRgb(0x55, 0x55, 0x55), '8' },
            { Color.FromRgb(0x55, 0x55, 0xFF), '9' },
            { Color.FromRgb(0x55, 0xFF, 0x55), 'a' },
            { Color.FromRgb(0x55, 0xFF, 0xFF), 'b' },
            { Color.FromRgb(0xFF, 0x55, 0x55), 'c' },
            { Color.FromRgb(0xFF, 0x55, 0xFF), 'd' },
            { Color.FromRgb(0xFF, 0xFF, 0x55), 'e' },
            { Color.FromRgb(0xFF, 0xFF, 0xFF), 'f' }
        };
        static readonly Dictionary<char, Color> CodeColor = new Dictionary<char, Color>
        {
            { '7', Color.FromRgb(0xAA, 0xAA, 0xAA) }, // color used by default

            { '0', Color.FromRgb(0x00, 0x00, 0x00) },
            { '1', Color.FromRgb(0x00, 0x00, 0xAA) },
            { '2', Color.FromRgb(0x00, 0xAA, 0x00) },
            { '3', Color.FromRgb(0x00, 0xAA, 0xAA) },
            { '4', Color.FromRgb(0xAA, 0x00, 0x00) },
            { '5', Color.FromRgb(0xAA, 0x00, 0xAA) },
            { '6', Color.FromRgb(0xFF, 0xAA, 0x00) },
            { '8', Color.FromRgb(0x55, 0x55, 0x55) },
            { '9', Color.FromRgb(0x55, 0x55, 0xFF) },
            { 'a', Color.FromRgb(0x55, 0xFF, 0x55) },
            { 'b', Color.FromRgb(0x55, 0xFF, 0xFF) },
            { 'c', Color.FromRgb(0xFF, 0x55, 0x55) },
            { 'd', Color.FromRgb(0xFF, 0x55, 0xFF) },
            { 'e', Color.FromRgb(0xFF, 0xFF, 0x55) },
            { 'f', Color.FromRgb(0xFF, 0xFF, 0xFF) }
        };

        // format codes
        static readonly Dictionary<CharacterStyle, char> FormatCode = new Dictionary<CharacterStyle, char>
        {
            { CharacterStyle.Obfuscated, 'k' },
            { CharacterStyle.Bold, 'l' },
            { CharacterStyle.Strikethrough, 'm' },
            { CharacterStyle.Underlined, 'n' },
            { CharacterStyle.Italic, 'o' },
            { CharacterStyle.Reset, 'r' }
        };
        static readonly Dictionary<char, CharacterStyle> CodeFormat = new Dictionary<char, CharacterStyle>
        {
            { 'k', CharacterStyle.Obfuscated },
            { 'l', CharacterStyle.Bold },
            { 'm', CharacterStyle.Strikethrough },
            { 'n', CharacterStyle.Underlined },
            { 'o', CharacterStyle.Italic },
            { 'r', CharacterStyle.Reset }
        };

        #endregion

        #region Text generation

        // generate the formatted text from a given box, with the specified prefix and the last style and color
        static string generateText(
            StylisedTextBox box,
            string prefix,
            ref CharacterStyle lastStyle,
            ref Color lastColor)
        {
            // if empty return empty
            var characters = box.GetStylisedCharacters();
            if (characters.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < characters.Count; i++)
            {
                // if it's not the first character, update the last style
                if (i > 0)
                {
                    lastStyle = characters[i - 1].Style;
                    lastColor = characters[i - 1].Color;
                }

                // if the character color has changed
                if (lastColor != characters[i].Color)
                {
                    sb.Append(prefix);
                    sb.Append(ColorCode[characters[i].Color]);

                    // the style is reset every time the color changes
                    // so if it's not the same as the initial style, update it
                    if (characters[i].Style != initialStyle)
                        sb.Append(getFormat(prefix, characters[i].Style));
                }
                // else if the character style has changed
                else if (lastStyle != characters[i].Style)
                {
                    sb.Append(prefix);
                    sb.Append(FormatCode[CharacterStyle.Reset]);
                    if (lastColor != initialColor)
                    {
                        sb.Append(prefix);
                        sb.Append(ColorCode[characters[i].Color]);
                    }

                    sb.Append(getFormat(prefix, characters[i].Style));
                }

                sb.Append(characters[i].Character);
            }

            return sb.ToString();
        }

        // get the specified string with a given prefix for the specified character
        static string getFormat(string prefix, CharacterStyle style)
        {
            var sb = new StringBuilder();

            // if reset, only return reset
            if (style == 0 || style.HasFlag(CharacterStyle.Reset))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Reset]);
                return sb.ToString();
            }

            if (style.HasFlag(CharacterStyle.Obfuscated))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Obfuscated]);
            }

            if (style.HasFlag(CharacterStyle.Bold))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Bold]);
            }

            if (style.HasFlag(CharacterStyle.Strikethrough))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Strikethrough]);
            }

            if (style.HasFlag(CharacterStyle.Underlined))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Underlined]);
            }

            if (style.HasFlag(CharacterStyle.Italic))
            {
                sb.Append(prefix);
                sb.Append(FormatCode[CharacterStyle.Italic]);
            }

            return sb.ToString();
        }

        List<StylisedCharacter> GetStylisedCharactersFromMOTD(
            string motd,
            int line,
            ref CharacterStyle lastStyle,
            ref Color lastColor)
        {
            if (line < 1 || line > 2)
                throw new ArgumentOutOfRangeException("The line must be between 1 and 2");

            var result = new List<StylisedCharacter>();
            for (int i = 0; i < motd.Length; i++)
            {
                var c = motd[i];
                if (c == '\\' && i < motd.Length - 1 && motd[i + 1] == 'n')
                {
                    --line;
                    ++i;
                    continue;
                }

                if (line != 1) // we're not ready to read
                    continue;

                if (c == '\\' && IsPropertiesStylePrefixNext(motd, i))
                {
                    i += propertiesStylePrefix.Length;
                    c = motd[i];

                    CharacterStyle style;
                    Color color;

                    if (CodeFormat.TryGetValue(c, out style))
                    {
                        if (style == CharacterStyle.Reset)
                        {
                            lastColor = ColorCode.First().Key; 
                            lastStyle = 0;
                        }
                        else
                            lastStyle |= style;
                    }
                    else if (CodeColor.TryGetValue(c, out color))
                    {
                        lastColor = color;
                        lastStyle = 0; // the style resets after a colour change
                    }
                }
                else
                    result.Add(new StylisedCharacter(c, lastColor, lastStyle));
            }
            return result;
        }

        static bool IsPropertiesStylePrefixNext(string motd, int index)
        {
            return motd.Substring(index).StartsWith(propertiesStylePrefix);
        }

        #endregion

        #region Get current values

        // get the currently selected color
        Color GetCurrentColor(int line)
        {
            return GetCurrentCharacter(line)?.Color ?? ColorCode.First().Key;
        }

        // gets the current coded style
        CharacterStyle GetCurrentCharacterStyle(int line)
        {
            return GetCurrentCharacter(line)?.Style ?? 0;
        }

        StylisedCharacter GetCurrentCharacter(int line)
        {
            StylisedTextBox box = null;
            if (line == 1)
                box = motdLine1;
            else if (line == 2)
                box = motdLine2;

            if (box != null && box.TextLength > 0)
            {
                int charIndex = box.CaretIndex - 1;
                if (charIndex < 0)
                    charIndex = 0;

                return box.GetStylisedCharacter(charIndex);
            }
            return null;
        }

        #endregion

        #region Private fields

        // line alignments
        TextAlignment _Line1Alignment;
        TextAlignment _Line2Alignment;

        // which box was the last focused?
        StylisedTextBox lastFocusedBox;

        #endregion

        #region Public properties

        public string GeneratedCode => generatedCode.Text;

        #endregion

        #region Public events

        public TextChangedEventHandler TextChanged { get; set; }
        void onTextChanged(TextChangedEventArgs e) => TextChanged?.Invoke(this, e);

        #endregion

        #region Line alignments

        // set the line alignment
        void setLineAlignment(int line, TextAlignment alignment)
        {
            if (alignment == TextAlignment.Justify)
                throw new NotSupportedException();

            switch (line)
            {
                case 1: _Line1Alignment = alignment; break;
                case 2: _Line2Alignment = alignment; break;
                default: throw new NotImplementedException();
            }

            updatePreviewAndResult();
        }

        // get the line alignment
        TextAlignment getLineAlignment(int line)
        {
            switch (line)
            {
                case 1: return _Line1Alignment;
                case 2: return _Line2Alignment;
                default: throw new NotImplementedException();
            }
        }

        #endregion

        #region Constructor

        public MOTDTextBox()
        {
            InitializeComponent();
            lastFocusedBox = motdLine1;
            
            foreach (var colorCode in ColorCode)
            {
                var button = new Button
                {
                    Background = new SolidColorBrush(colorCode.Key),
                    Tag = colorCode.Key,
                    Style = FindResource("FlatSquareButton") as Style
                };
                button.Click += colorButtonClick;

                codedColorsPanel.Children.Add(button);
            }
            // check the default color
            //((RadioButton)codedColorsPanel.Children[0]).IsChecked = true;

            foreach (Button modeCheckbox in modePanel.Children)
                modeCheckbox.Click += styleButtonClick;

            generatedCode.TextChanged += (s, e) => onTextChanged(e);
        }

        #endregion

        #region Style changed

        void colorButtonClick(object sender, RoutedEventArgs e)
        {
            lastFocusedBox.Focus();

            motdLine1.CurrentColor = motdLine2.CurrentColor = (Color)((Button)sender).Tag;

            updatePreviewAndResult();
        }

        void styleButtonClick(object sender, RoutedEventArgs e)
        {
            lastFocusedBox.Focus();

            motdLine1.ToggleStyleFlag((CharacterStyle)((Button)sender).Tag);
            motdLine2.ToggleStyleFlag((CharacterStyle)((Button)sender).Tag);

            checkBoxLength(motdLine1, false);
            checkBoxLength(motdLine2, false);

            updatePreviewAndResult();
        }

        #endregion

        #region Events

        // text changed
        void motd_Changed(object sender, TextChangedEventArgs e)
        {
            checkBoxLength((StylisedTextBox)sender, true);
        }

        void checkBoxLength(StylisedTextBox box, bool updatePreview)
        {
            if (calculatePixelsLength(box) >= 1)
                --box.TextLength;

            if (updatePreview)
                updatePreviewAndResult();
        }

        // motd box focused or lost focus
        void motd_Focused(object sender, RoutedEventArgs e)
        {
            lastFocusedBox = (StylisedTextBox)sender;
        }

        void motd_Unfocused(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        // set allignmentss
        void leftAlignment1_Click(object sender, RoutedEventArgs e) => setLineAlignment(1, TextAlignment.Left);
        void centerAlignment1_Click(object sender, RoutedEventArgs e) => setLineAlignment(1, TextAlignment.Center);
        void rightAlignment1_Click(object sender, RoutedEventArgs e) => setLineAlignment(1, TextAlignment.Right);

        void leftAlignment2_Click(object sender, RoutedEventArgs e) => setLineAlignment(2, TextAlignment.Left);
        void centerAlignment2_Click(object sender, RoutedEventArgs e) => setLineAlignment(2, TextAlignment.Center);
        void rightAlignment2_Click(object sender, RoutedEventArgs e) => setLineAlignment(2, TextAlignment.Right);

        // copy generated text
        void copyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(generatedCode.Text);
        }

        #endregion

        #region Preview

        // update the motd preview and its result
        void updatePreviewAndResult()
        {
            if (motdLine1.TextLength == 0 && motdLine2.TextLength == 0)
            {
                motdResultLine1.Text = motdResultLine2.Text = generatedCode.Text = string.Empty;
            }
            else
            {
                performAlignmentAdjustment(
                    motdResultLine1,
                    motdLine1.GetStylisedCharacters(),
                    getLineAlignment(1));

                CharacterStyle lastStyle = 0;
                Color lastColor = ColorCode.First().Key;
                var result = generateText(motdResultLine1, propertiesStylePrefix, ref lastStyle, ref lastColor);

                if (motdLine2.TextLength > 0)
                {
                    performAlignmentAdjustment(
                        motdResultLine2,
                        motdLine2.GetStylisedCharacters(),
                        getLineAlignment(2));

                    result += @"\n";
                    result += generateText(motdResultLine2, propertiesStylePrefix, ref lastStyle, ref lastColor);
                }

                generatedCode.Text = result.Trim();
            }
        }

        #endregion

        #region Public methods

        // reverse motd generation process
        public void SetMOTD(string motd)
        {
            CharacterStyle lastStyle = 0;
            Color lastColor = ColorCode.First().Key;
            motdLine1.SetStylisedCharacters(GetStylisedCharactersFromMOTD(motd, 1, ref lastStyle, ref lastColor));
            motdLine2.SetStylisedCharacters(GetStylisedCharactersFromMOTD(motd, 2, ref lastStyle, ref lastColor));
            updatePreviewAndResult();
        }

        #endregion

        #region Adjut lines

        // perfoms a line adjustment (adds white spaces if the box needs them) on a stylised textbox
        static void performAlignmentAdjustment(
            StylisedTextBox box,
            List<StylisedCharacter> characters,
            TextAlignment alignment)

        {
            box.SetStylisedCharacters(characters);

            double boxPixelsLength = calculatePixelsLength(box);
            double remainingPixels = maxPixelLength - boxPixelsLength;

            if (characters.Count == 0)
                return;

            switch (alignment)
            {
                case TextAlignment.Right:

                    box.InsertText(0, getMaxAvailableStringRoundingTop(remainingPixels, ' ', false),
                        ColorCode.First().Key, CharacterStyle.Reset);

                    break;

                case TextAlignment.Center:

                    box.InsertText(0,
                        getMaxAvailableStringRoundingTop(remainingPixels / 2d, ' ', false),
                        ColorCode.First().Key, CharacterStyle.Reset);

                    break;

                default:
                    // do nothing
                    break;
            }
        }

        #endregion

        #region Utils
        
        // get a new string of the given character that fills all the available pixels
        static string getMaxAvailableStringRoundingTop(double availablePixels, char c, bool bold)
        {
            var charPixels = getCharacterPixelSize(c, bold);
            return new string(c, (int)(availablePixels / charPixels));
        }

        // calculates the current stylised text box text length in pixels
        static double calculatePixelsLength(StylisedTextBox box)
        {
            double result = 0;

            var characters = box.GetStylisedCharacters();
            foreach (var character in characters)
                result += getCharacterPixelSize(
                    character.Character, character.Style.HasFlag(CharacterStyle.Bold));

            return result;
        }

        #endregion
    }
}