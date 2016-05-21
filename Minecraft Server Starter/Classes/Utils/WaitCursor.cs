/// <copyright file="WaitCursor.cs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Nicholas Butler</author>
/// <date>02/03/2004</date>
/// <summary>Class used to use a wait cursor easily (using (WaitCursor.New) ...)</summary>

using System;
using System.Windows.Input;

public class WaitCursor : IDisposable
{
    Cursor _previousCursor;

    public WaitCursor()
    {
        _previousCursor = Mouse.OverrideCursor;
        Mouse.OverrideCursor = Cursors.Wait;
    }

    public void Dispose()
    {
        Mouse.OverrideCursor = _previousCursor;
    }

    public static WaitCursor New => new WaitCursor();
}