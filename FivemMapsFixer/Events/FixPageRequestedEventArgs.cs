using System;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.Events;

public class FixPageRequestedEventArgs(FileType fileType) : EventArgs
{
    public FileType FileType { get; } = fileType;
}