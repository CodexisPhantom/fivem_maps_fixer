using System;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.Events;

public class ConflictEventArgs:EventArgs
{
    public Issue Issue { get; }
    public ConflictEventArgs(Issue issue)
    {
        Issue = issue;
    }
}