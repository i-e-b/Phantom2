﻿using System.Linq;
using Phantom.Parsers.Interfaces;
using Phantom.Results;

namespace Phantom.Parsers.Terminals;

/// <summary>
/// Match a single character that is in a range, and is not in the list of exclusions
/// </summary>
public class RangeExcludingCharacterSet : Parser, IMatchingParser
{
    private readonly char _lower;
    private readonly char _upper;
    private readonly char[] _exclusions;

    /// <summary>
    /// Match a single character that is between <paramref name="lower"/>
    /// and <paramref name="upper"/> (inclusive), which is not in the list of exclusions
    /// </summary>
    public RangeExcludingCharacterSet(char lower, char upper, params char[] exclusions)
    {
        _lower = lower;
        _upper = upper;
        _exclusions = exclusions;
    }

    /// <inheritdoc />
    public ParserMatch TryMatch(IScanner scan, ParserMatch? previousMatch)
    {
        var offset = previousMatch?.Right ?? 0;
        if (scan.EndOfInput(offset)) return scan.NoMatch(this, previousMatch);

        char c = scan.Peek(offset);

        
        if (c < _lower || c > _upper|| _exclusions.Contains(c)) return scan.NoMatch(this, previousMatch);

        // if we arrive at this point, we have a match
        return scan.CreateMatch(this, offset, 1);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var desc = "{'"+_lower+"'..'"+_upper+"'; NOT '"
                   + string.Join("','",_exclusions.Select(c=>c.ToString())) + "'}";

        if (Tag is null) return desc;
        return desc + " Tag='" + Tag + "'";
    }
    
    /// <inheritdoc />
    public override string ShortDescription(int depth)
    {
        return ToString();
    }
}