/// <copyright file="StylisedTextBox.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>19/03/2016</date>
/// <summary>Stylised textbox, with character styling and colour supported</summary>

// Pending:
//  - Multiline support
//  - Handling text over the textbox width
//  - Testing with other sizes
//  - Optimise obfuscated timer (only run when there are added, don't check always; recheck all characters after removing some obfuscated character)
//  - Optimise glyph runs, by using an equal glyphrun for similar consecuent characters (see https://smellegantcode.wordpress.com/2008/07/03/glyphrun-and-so-forth/)
//  - Improve CaretIndex position after setting TextLength

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Minecraft_Server_Starter
{
    #region Public enumerations

    /// <summary>
    /// Specifies the character style
    /// </summary>
    [Flags]
    public enum CharacterStyle
    {
        Reset = 1 << 0,
        Bold = 1 << 1,
        Italic = 1 << 2,
        Underlined = 1 << 3,
        Strikethrough = 1 << 4,
        Obfuscated = 1 << 5,
    }

    #endregion

    #region Main class

    /// <summary>
    /// An improved TextBox which uses stylised characters instead
    /// </summary>
    public class StylisedTextBox : TextBox
    {
        #region Private fields and properties

        // don't create the GlyphTypeface's everytime
        Dictionary<CharacterStyle, GlyphTypeface> styleTypeface;

        // timer used to make an "obfuscated text effect"
        DispatcherTimer obfuscatedTimer;

        // where the glyphs should start to be rendered
        // might not always be this value, but it works :-)
        Point _origin = new Point(3, 12);
        Point origin
        {
            get { return _origin; }
            set
            {
                if (_origin != value)
                {
                    _origin = value;
                    InvalidateVisual();
                }
            }
        }

        // characters of the text box
        List<StylisedCharacter> characters = new List<StylisedCharacter>();

        #endregion

        #region Public properties

        CharacterStyle _CurrentStyle = 0;
        /// <summary>
        /// Gets or sets the current text style. Both the current selection and new added text will use this style
        /// </summary>
        public CharacterStyle CurrentStyle
        {
            get { return _CurrentStyle; }
            set
            {
                if (_CurrentStyle != value)
                {
                    _CurrentStyle = value;
                    updateSelectionStyle();
                }
            }
        }

        Color _CurrentColor = Colors.Black;
        /// <summary>
        /// Gets or sets the current foreground color. Both the current selection and new added text will use this color
        /// </summary>
        public Color CurrentColor
        {
            get { return _CurrentColor; }
            set
            {
                if (_CurrentColor != value)
                {
                    _CurrentColor = value;
                    updateSelectionColor();
                }
            }
        }

        /// <summary>
        /// Gets or sets the plain text of the stylised text box
        /// </summary>
        public new string Text
        {
            get
            {
                return new string(characters.Select(s => s.Character).ToArray());
            }
            set
            {
                characters.Clear();
                if (value.Length == 0)
                    InvalidateVisual();
                else
                    addText(value);
            }
        }

        /// <summary>
        /// Gets or sets the text length. If setting, you can only reduce the amount of characters
        /// </summary>
        public int TextLength
        {
            get { return characters.Count; }
            set
            {
                if (value >= characters.Count)
                    throw new NotImplementedException();
                
                characters.RemoveRange(value, characters.Count - value);
                base.Text = base.Text.Substring(0, value);
                CaretIndex = value;

                InvalidateVisual();
            }
        }

        // override properties to make sure onrender is called
        public new FontFamily FontFamily
        {
            get { return base.FontFamily; }
            set
            {
                base.FontFamily = value;
                genGlyphTypefaces();
                InvalidateVisual();
            }
        }
        public new FontStretch FontStretch
        {
            get { return base.FontStretch; }
            set
            {
                base.FontStretch = value;
                genGlyphTypefaces();
                InvalidateVisual();
            }
        }


        #endregion

        #region Constructor

        public StylisedTextBox()
        {
            FontFamily = new FontFamily("Consolas");
            // if the upper line is removed, remember to uncomment this!:
            // genGlyphTypefaces();

            // it has to be transparent or the text won't be rendered
            Background = Foreground = Brushes.Transparent;

            // set timers
            obfuscatedTimer = new DispatcherTimer
                (TimeSpan.FromMilliseconds(40), DispatcherPriority.Background, obfuscatedTick, Dispatcher.CurrentDispatcher);

            DataObject.AddPastingHandler(this, OnPaste);

            // we do not implement undo/redo commands
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, null, cancelCmdExecution));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, null, cancelCmdExecution));

            // these commands delete the currently selected text
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, deleteCmdExecution));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, null, deleteCmdExecution));
        }

        #endregion

        #region Command handling

        void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText) return;

            var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            addText(text);
        }

        void cancelCmdExecution(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;
        }

        void deleteCmdExecution(object sender, CanExecuteRoutedEventArgs e)
        {
            checkDeletion();
        }

        #endregion

        #region Text management

        #region Text input overrides

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case Key.Space:
                    addText(" ");
                    e.Handled = false;
                    break;

                case Key.Delete:
                case Key.Back:
                    deleteSelection(e.Key);
                    e.Handled = false;
                    break;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (e.Handled)
                return;

            addText(e.Text);
            base.OnPreviewTextInput(e);
        }

        #endregion

        #region Text deletion

        // if there's a selection, delete it
        void checkDeletion()
        {
            if (SelectionLength > 0)
                deleteSelection(0);
        }

        // delete the current selection, given a key
        void deleteSelection(Key usedKey)
        {
            if (characters.Count > 0)
            {
                if (SelectionLength > 0)
                    characters.RemoveRange(SelectionStart, SelectionLength);

                else
                {
                    if (usedKey == Key.Delete)
                        characters.RemoveAt(CaretIndex);

                    else if (CaretIndex > 0) // && usedKey == Key.Back
                        characters.RemoveAt(CaretIndex - 1);
                }

                InvalidateVisual();
            }
        }

        #endregion

        #region Text insertion

        // adds the typed text at the caretindex index
        void addText(string text) => InsertText(CaretIndex, text, false);

        #endregion

        #region Text styling

        // update the current selection color and style
        void updateSelectionColor()
        {
            if (SelectionLength == 0)
                return;

            var to = SelectionStart + SelectionLength;
            for (int i = SelectionStart; i < to; i++)
                characters[i].Color = _CurrentColor;

            InvalidateVisual();
        }

        // update the current selection color and style
        void updateSelectionStyle()
        {
            if (SelectionLength == 0)
                return;

            var to = SelectionStart + SelectionLength;
            for (int i = SelectionStart; i < to; i++)
                characters[i].Style = _CurrentStyle;

            InvalidateVisual();
        }

        // obfuscated timer
        void obfuscatedTick(object sender, EventArgs e)
        {
            bool any = false;

            foreach (var character in characters)
                if (character.Style.HasFlag(CharacterStyle.Obfuscated))
                {
                    any = true;
                    break;
                }

            if (any)
                InvalidateVisual();
        }

        #endregion

        #endregion

        #region Public methods

        /// <summary>
        /// Toggles an style flag, for example, if there was bold and flag is italic, 
        /// result will be bold and italic. If bold is toggled after, the flag will be italic
        /// </summary>
        /// <param name="style">The style to toggle</param>
        public void ToggleStyleFlag(CharacterStyle style)
        {
            if (style == CharacterStyle.Reset)
                CurrentStyle = 0;

            else
            {
                if (_CurrentStyle.HasFlag(style)) // toggle off with XOR
                    CurrentStyle = _CurrentStyle ^ style;
                else
                    CurrentStyle |= style; // toggle on with OR
            }
        }

        #endregion

        #region Text insertion and retrieval

        /// <summary>
        /// Appends a string to the end of the content of this text box
        /// </summary>
        /// <param name="text">The text to append</param>
        public new void AppendText(string text)
        {
            AppendText(text, true);
        }
        void AppendText(string text, bool updateBaseText)
        {
            InsertText(characters.Count, text, updateBaseText);
        }

        /// <summary>
        /// Appends a string to the end of the content of this text box, with a specified color and style
        /// </summary>
        /// <param name="text">The text to append</param>
        /// <param name="color">The color to be used</param>
        /// <param name="style">The style to be used</param>
        public void AppendText(string text, Color color, CharacterStyle style)
        {
            AppendText(text, color, style, true);
        }
        void AppendText(string text, Color color, CharacterStyle style, bool updateBaseText)
        {
            InsertText(characters.Count, text, color, style, updateBaseText);
        }

        /// <summary>
        /// Appends a string at the specified index
        /// </summary>
        /// <param name="index">The desired index</param>
        /// <param name="text">The text to append</param>
        public void InsertText(int index, string text)
        {
            InsertText(index, text, true);
        }
        public void InsertText(int index, string text, bool updateBaseText)
        {
            InsertText(index, text, _CurrentColor, _CurrentStyle, updateBaseText);
        }

        /// <summary>
        /// Appends a string at the specified index
        /// </summary>
        /// <param name="index">The desired index</param>
        /// <param name="text">The text to append</param>
        /// <param name="color">The color to be used</param>
        /// <param name="style">The style to be used</param>
        public void InsertText(int index, string text, Color color, CharacterStyle style)
        {
            InsertText(index, text, color, style, true);
        }
        void InsertText(int index, string text, Color color, CharacterStyle style, bool updateBaseText)
        {
            if (string.IsNullOrEmpty(text))
                return;

            InsertStylisedCharacters(index, StylisedCharacter.FromString(text, color, style), updateBaseText);
        }

        /// <summary>
        /// Sets the stylised characters
        /// </summary>
        /// <param name="chars">The stylised characters to set</param>
        public void SetStylisedCharacters(List<StylisedCharacter> chars)
        {
            SetStylisedCharacters(chars, true);
        }
        public void SetStylisedCharacters(List<StylisedCharacter> chars, bool updateBaseText)
        {
            characters.Clear();
            InsertStylisedCharacters(0, chars, updateBaseText);
        }

        /// <summary>
        /// Inserts stylised characters at the specified index
        /// </summary>
        /// <param name="index">The index in which the characters will be inserted</param>
        /// <param name="chars">The stylised characters to insert</param>
        public void InsertStylisedCharacters(int index, List<StylisedCharacter> chars)
        {
            InsertStylisedCharacters(index, chars, true);
        }
        void InsertStylisedCharacters(int index, List<StylisedCharacter> chars, bool updateBaseText)
        {
            if (chars == null || chars.Count == 0)
                return;

            if (index < 0 || index > characters.Count)
                return;

            checkDeletion();
            
            int maxLength = MaxLength > 0 ? MaxLength : int.MaxValue;
            if (characters.Count + chars.Count <= maxLength)
            {
                characters.InsertRange(index, chars);
            }

            else if (characters.Count < maxLength)
            {
                var exceeds = maxLength - characters.Count; // TODO test this, old code used "chars" below instead "characters"
                characters.RemoveRange(characters.Count - exceeds, exceeds);
            }

            if (updateBaseText)
                base.Text = Text;

            InvalidateVisual();
        }

        /// <summary>
        /// Retrieves the stylised characters
        /// </summary>
        /// <returns>The stylised characters</returns>
        public List<StylisedCharacter> GetStylisedCharacters() => new List<StylisedCharacter>(characters);

        /// <summary>
        /// Retrieves the stylised at the given index
        /// </summary>
        /// <returns>The stylised character</returns>
        public StylisedCharacter GetStylisedCharacter(int index) => characters[index].Clone();

        #endregion

        #region GlyphTypefaces generation

        void genGlyphTypefaces()
        {
            styleTypeface = new Dictionary<CharacterStyle, GlyphTypeface>(4);

            styleTypeface.Add(0, getGlyphTypeface(0));

            styleTypeface.Add(CharacterStyle.Italic, getGlyphTypeface(CharacterStyle.Italic));
            styleTypeface.Add(CharacterStyle.Bold, getGlyphTypeface(CharacterStyle.Bold));

            styleTypeface.Add(
                CharacterStyle.Italic | CharacterStyle.Bold,
                getGlyphTypeface(CharacterStyle.Italic | CharacterStyle.Bold));
        }

        GlyphTypeface getGlyphTypeface(CharacterStyle style)
        {
            FontStyle fstyle = (style & CharacterStyle.Italic) != 0 ? FontStyles.Italic : FontStyles.Normal;
            FontWeight fweight = (style & CharacterStyle.Bold) != 0 ? FontWeights.Bold : FontWeights.Normal;

            var typeface = new Typeface(FontFamily, fstyle, fweight, FontStretch);

            GlyphTypeface glyphTypeface;
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                throw new InvalidOperationException("No GlyphTypeface found. Please use another font");

            return glyphTypeface;
        }

        #endregion

        #region Render

        protected override void OnRender(DrawingContext dc)
        {
            if (characters.Count == 0)
                return; // don't render anything

            var currentPos = origin;
            for (int i = 0; i < characters.Count; i++)
            {
                // get a glyph style that only handles both italic and bold
                CharacterStyle glyphStyle = 0;
                if (characters[i].Style.HasFlag(CharacterStyle.Italic))
                    glyphStyle |= CharacterStyle.Italic;

                if (characters[i].Style.HasFlag(CharacterStyle.Bold))
                    glyphStyle |= CharacterStyle.Bold;

                // retrieve the corresponding type face
                var glyphTypeface = styleTypeface[glyphStyle];

                // get basic values
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[characters[i].Character];
                double width = glyphTypeface.AdvanceWidths[glyphIndex] * FontSize;

                // create lists
                var glyphIndices = new List<ushort>(1);
                glyphIndices.Add(glyphIndex);

                var advanceWidths = new List<double>(1);
                advanceWidths.Add(width);

                // create glyph run
                var glyphRun = new GlyphRun(
                    glyphTypeface,
                    0,
                    false,
                    FontSize,
                    glyphIndices,
                    currentPos,
                    advanceWidths,
                    null, null, null, null, null, null);

                // draw it
                dc.DrawGlyphRun(new SolidColorBrush(characters[i].Color), glyphRun);

                if (characters[i].Style.HasFlag(CharacterStyle.Underlined))
                {
                    // draw an underline
                    var y = currentPos.Y +
                        (glyphTypeface.Height - glyphTypeface.Baseline) * FontSize;

                    dc.DrawLine(new Pen(new SolidColorBrush(characters[i].Color), 1),
                        new Point(currentPos.X, y),
                        new Point(currentPos.X + width, y));
                }

                if (characters[i].Style.HasFlag(CharacterStyle.Strikethrough))
                {
                    // draw a strikethrough
                    var y = currentPos.Y +
                        (glyphTypeface.Baseline - glyphTypeface.Height) * FontSize;

                    dc.DrawLine(new Pen(new SolidColorBrush(characters[i].Color), 1),
                        new Point(currentPos.X, y),
                        new Point(currentPos.X + width, y));
                }

                // advance the current position
                currentPos.X += width;
            }
        }

        #endregion
    }

    #endregion

    #region Required sub classes

    /// <summary>
    /// Improved character with support for color and character style
    /// </summary>
    public class StylisedCharacter
    {
        // used for obfuscated text
        static readonly Random r = new Random();

        // original character value
        char _Character;
        /// <summary>
        /// Gets or sets the character value of this stylised character
        /// </summary>
        public char Character
        {
            get
            {
                if (Style.HasFlag(CharacterStyle.Obfuscated) && _Character != ' ')
                    return (char)r.Next(33, 127);

                return _Character;
            }
            set { _Character = value; }
        }

        /// <summary>
        /// Gets or sets the character foreground color
        /// </summary>
        public Color Color { get; set; }

        CharacterStyle _Style;
        /// <summary>
        /// Gets or sets the character style
        /// </summary>
        public CharacterStyle Style
        {
            get { return _Style; }
            set
            {
                if (_Style != value)
                {
                    // if it has reset, ONLY reset
                    if ((value & CharacterStyle.Reset) != 0)
                        _Style = CharacterStyle.Reset;

                    // else set the style
                    else
                        _Style = value;
                }
            }
        }

        /// <summary>
        /// Creates a new stylised character style, specifying a color and a style
        /// </summary>
        /// <param name="c">The character value</param>
        /// <param name="color"></param>
        /// <param name="style"></param>
        public StylisedCharacter(char c, Color color, CharacterStyle style)
        {
            Character = c;
            Color = color;
            Style = style;
        }

        /// <summary>
        /// Creates a new character style with no defined style
        /// </summary>
        /// <param name="c">The character value</param>
        public StylisedCharacter(char c)
        {
            Character = c;
            Color = Colors.Black;
        }

        /// <summary>
        /// Creates an stylised character array from a given string, provided a color and a style
        /// </summary>
        /// <param name="str">The string value from which the stylised character array will be created</param>
        /// <param name="color">The foreground color for all the characters</param>
        /// <param name="style">The style for all the characters</param>
        /// <returns>The generated array</returns>
        public static List<StylisedCharacter> FromString(string str, Color color, CharacterStyle style)
        {
            var value = new List<StylisedCharacter>(str.Length);
            foreach (var c in str)
                value.Add(new StylisedCharacter(c, color, style));

            return value;
        }

        public StylisedCharacter Clone()
        {
            return new StylisedCharacter(_Character, Color, Style);
        }

        // conversion operators
        public static explicit operator char(StylisedCharacter c) => c.Character;
        public static implicit operator StylisedCharacter(char c) => new StylisedCharacter(c);
    }

    #endregion
}