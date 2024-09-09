﻿using Gool.Parsers.Interfaces;
using Gool.Results;

namespace Gool.Parsers.Terminals;

/// <summary>
/// Matches all remaining input if its length is between min and max (inclusive)
/// </summary>
public class RemainingLength : Parser, IMatchingParser
{
    private readonly int _min;
    private readonly int _max;

    /// <summary>
    /// Matches all remaining input if its length is between min and max (inclusive)
    /// </summary>
    public RemainingLength(int min, int max)
    {
        _min = min;
        _max = max;
    }

    /// <summary>
    /// Test the regular expression.
    /// </summary>
    /// <remarks>This is done on the entire input.
    /// This might cause problems with file-stream parsing.</remarks>
    public ParserMatch TryMatch(IScanner scan, ParserMatch? previousMatch)
    {
        var offset = previousMatch?.Right ?? 0;

        var remainingLength = scan.InputString.Length - offset;//RemainingData(offset).Length;

        if (remainingLength < _min || remainingLength > _max) return scan.NoMatch(this, previousMatch);

        return scan.CreateMatch(this, offset, remainingLength);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var desc = $"[*{_min}..{_max}]";
			
        if (Tag is null) return desc;
        return desc + " Tag='" + Tag + "'";
    }
    
    /// <inheritdoc />
    public override string ShortDescription(int depth)
    {
        return ToString();
    }
}