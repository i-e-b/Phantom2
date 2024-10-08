using System;
using System.Collections.Generic;
using Gool.Parsers;
using Gool.Results;

namespace Gool.Scanners;

/// <summary>
/// Scanner that operates over strings.
/// </summary>
public class ScanStrings : IScanner
{
    private bool    _completed;
    private string? _transformedString;
    private string? _furthestTag;

    private readonly string                      _input;
    private readonly List<ParserPoint>           _failurePoints     = new();
    private readonly Dictionary<object, object?> _contexts          = new();
    private readonly HashSet<string>             _failedTags        = new();
    private readonly int                         _inputLength;

    /// <summary>
    /// Create a new scanner from an input string.
    /// </summary>
    /// <param name="input">String to scan</param>
    public ScanStrings(string input)
    {
        _input = input;
        _inputLength = input.Length;
        FurthestOffset = 0;
        _completed = false;

        Transform = new NoTransform();
    }

    /// <summary>
    /// If <c>true</c>, auto-advanced elements (like white-space skips)
    /// will be added to the result tree.
    /// </summary>
    public bool IncludeSkippedElements { get; set; }

    /// <summary>
    /// Get the original input string
    /// </summary>
    public string InputString => _input;

    /// <summary>
    /// The input string, as processed by the transformer.
    /// This will be equal to the input string if there is no transformer.
    /// </summary>
    public string TransformedString
    {
        get
        {
            _transformedString ??= Transform.Transform(_input);
            return _transformedString;
        }
    }

    #region IScanner Members

    /// <summary>
    /// Add a success path, for diagnostic use
    /// </summary>
    public void AddSuccess(ParserMatch newMatch)
    {
        if (newMatch.Right > (FurthestMatch?.Right ?? 0)) FurthestMatch = newMatch;
        _furthestTag = LastTag;
        _failurePoints.Clear();
        _failedTags.Clear();
    }

    /// <inheritdoc />
    public void Complete()
    {
        _completed = true;
    }

    /// <inheritdoc />
    public void SetContext(IParser parser, object? context)
    {
        _contexts[parser] = context;
    }

    /// <inheritdoc />
    public object? GetContext(IParser parser)
    {
        return _contexts.GetValueOrDefault(parser);
    }

    /// <inheritdoc />
    public void AddFailure(IParser failedParser, ParserMatch failMatch)
    {
        if (failMatch.Right > (FurthestTest?.Right ?? 0)) FurthestTest = failMatch;
        if (LastTag is not null) _failedTags.Add(LastTag);
        _failurePoints.Add(new ParserPoint(failedParser, failMatch, this));
    }

    /// <inheritdoc />
    public List<string> ListFailures(int minimumOffset = 0, bool showDetails = false)
    {
        var lst = new List<string>();

        if (FurthestTest is not null)
        {
            if (_failedTags.Count > 0)
            {
                lst.Add($"Expected '{string.Join("', '", _failedTags)}'");
            }

            if (_furthestTag is not null) lst.Add($"After '{_furthestTag}'");

            var offset = FurthestMatch?.Offset ?? 0;
            var prev   = UntransformedSubstring(0, offset);
            var length = Math.Max(0, (FurthestTest?.Right ?? _input.Length) - offset);
            var left   = UntransformedSubstring(offset, length);
            var right  = UntransformedSubstring(offset + length, _input.Length);
            lst.Add($"{prev}◢{left}◣{right}");
        }

        if (!showDetails) return lst;


        foreach (var p in _failurePoints)
        {
            if (p.Offset < minimumOffset) continue;

            var prev  = _input[..p.Offset];
            var left  = p.Length >= 0 ? _input.Substring(p.Offset, p.Length) : "";
            var right = _input[(p.Offset + p.Length)..];

            lst.Add($"{prev}◢{left}◣{right} --> ({FurthestMatch?.Offset ?? 0},{FurthestMatch?.Right ?? 0}..{FurthestTest?.Offset ?? 0},{FurthestTest?.Right ?? 0}) {ParserStringFrag(p)}");
        }

        return lst;
    }

    private static string ParserStringFrag(ParserPoint p)
    {
        if (!string.IsNullOrWhiteSpace(p.Parser.Tag)) return p.Parser.Tag;
        var str = p.Parser.ShortDescription(depth: 7);
        return str;
    }

    /// <inheritdoc />
    public bool EndOfInput(int offset)
    {
        return offset >= _inputLength;
    }

    /// <inheritdoc />
    public bool Read(ref int offset)
    {
        if (_completed) throw new Exception("This scanner has been completed");
        if (EndOfInput(offset)) return false;

        offset++;

        return !EndOfInput(offset);
    }

    /// <inheritdoc />
    public char Peek(int offset)
    {
        if (offset >= _inputLength) return (char)0;
        return TransformedString[offset];
    }

    /// <summary>
    /// If skip whitespace is set and current position is whitespace,
    /// seek forward until on non-whitespace position or EOF.
    /// </summary>
    public ParserMatch? DoAutoAdvance(ParserMatch? previous)
    {

        /*
         *
       if (!SkipWhitespace) return previous;

       var left = previous?.Right ?? 0;
       if (EndOfInput(left)) return previous;

       var ws     = NullMatch(_autoAdvanceParser, left, previous);
       var offset = ws.Right;
       var c      = Peek(offset);

       while (char.IsWhiteSpace(c)) // if this is whitespace
       {
           ws.ExtendTo(offset + 1); // mark our match up to this character
           if (!Read(ref offset)) break; // try to advance to next character
           c = Peek(offset); // read that character
       }
       // It's very important to have auto-advance off!
       if (AutoAdvance is null) return previous;

       return ws;

         */
        // It's very important to have auto-advance off!
        if (AutoAdvance is null) return previous;


        var left = previous?.Right ?? 0;
        var prev   = NullMatch(AutoAdvance, left, previous);
        if (EndOfInput(left)) return prev;

        var skipMatch = AutoAdvance.Parse(this, prev, allowAutoAdvance: false);
        return (skipMatch.Length > 0) ? skipMatch : prev;
    }

    /// <inheritdoc />
    public ReadOnlySpan<char> Substring(int offset, int length)
    {
        return TransformedString.AsSpan(offset, Math.Min(length, _inputLength - offset));
    }

    /// <inheritdoc />
    public int IndexOf(int offset, string toFind, StringComparison comparisonType)
    {
        return TransformedString.IndexOf(toFind, offset, comparisonType);
    }

    /// <inheritdoc />
    public string UntransformedSubstring(int offset, int length)
    {
        if (length == 0) return "";
        if (length > 0) return InputString.Substring(offset, Math.Min(length, _inputLength - offset));

        // Caller has asked for negative length, which we handle as moving the offset back
        var left = Math.Max(0, offset + length);
        length = offset - left;
        return InputString.Substring(left, length);
    }

    /// <inheritdoc />
    public ITransform Transform { get; set; }

    /// <inheritdoc />
    public IParser? AutoAdvance { get; set; }

    /// <inheritdoc />
    public int FurthestOffset { get; private set; }

    /// <inheritdoc />
    public ParserMatch? FurthestMatch { get; private set; }

    /// <inheritdoc />
    public ParserMatch? FurthestTest { get; private set; }

    /// <inheritdoc />
    public string? LastTag { get; set; }

    /// <inheritdoc />
    public ParserMatch NoMatch(IParser source, ParserMatch? previous)
    {
        return new ParserMatch(source, this, previous?.Offset ?? 0, -1, previous);
    }

    /// <inheritdoc />
    public ParserMatch EmptyMatch(IParser source, int offset,ParserMatch? previous)
    {
        return new ParserMatch(source, this, offset, 0, previous);
    }

    /// <inheritdoc />
    public ParserMatch NullMatch(IParser source, int offset,ParserMatch? previous)
    {
        return new ParserMatch(source, this, offset, -1, previous);
    }

    /// <inheritdoc />
    public ParserMatch CreateMatch(IParser source, int offset, int length, ParserMatch? previous)
    {
        if ((offset + length) > FurthestOffset)
        {
            FurthestOffset = offset + length;
        }

        return new ParserMatch(source, this, offset, length, previous);
    }

    #endregion
}