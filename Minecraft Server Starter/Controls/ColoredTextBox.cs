/// <copyright file="ColoredTextBox.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Simple class to generated colored text</summary>

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Minecraft_Server_Starter
{
    class ColoredTextBox : RichTextBox
    {
        Paragraph paragraph;

        #region Public properties

        // this isn't exactly how it should work for a final product but... serves our purpose
        public string Text
        {
            get
            {
                if (Selection.IsEmpty)
                    return new TextRange(Document.ContentStart, Document.ContentEnd).Text.Trim();

                return Selection.Text.Trim();
            }
        }

        #endregion

        #region Constructor

        public ColoredTextBox()
        {
            Clear();
        }

        #endregion

        #region Public methods

        // append text
        public new void AppendText(string text)
        {
            paragraph.Inlines.Add(new Run(text));
        }
        public void AppendText(string text, Brush color)
        {
            paragraph.Inlines.Add(new Run(text) { Foreground = color });
        }

        // append lines
        public void AppendLine(string text)
        {
            AppendText(text);
            paragraph.Inlines.Add(new LineBreak());
        }
        public void AppendLine(string text, Brush color)
        {
            AppendText(text, color);
            paragraph.Inlines.Add(new LineBreak());
        }

        // clear the paragraph and add a new clean one
        public void Clear()
        {
            paragraph = new Paragraph();
            Document = new FlowDocument(paragraph);
        }

        #endregion
    }
}
