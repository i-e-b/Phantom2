﻿namespace Phantom;

/// <summary>
/// Change in tree scope that this match represents
/// </summary>
public enum ScopeType
{
    /// <summary> This match is general data </summary>
    None = 0,
	
    /// <summary> This match should become a parent to its siblings </summary>
    Pivot = 2,
	
    /// <summary> This match is the start of a block </summary>
    OpenScope = 1,
	
    /// <summary> This match is the end of a block </summary>
    CloseScope = -1,
}