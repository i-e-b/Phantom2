﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Phantom.Results;

/// <summary>
/// Hierarchy node for scoped-and-tagged parser matches.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class ScopeNode
{
    /// <summary>
    /// Type of node
    /// </summary>
    public ScopeNodeType NodeType { get; private set; }

    /// <summary>
    /// ParserMatch that represents a tagged token that exists within its scope.
    /// <c>null</c> if this is a scope change.
    /// </summary>
    public ParserMatch? DataMatch { get; private set; }

    /// <summary>
    /// ParserMatch that opened the current scope.
    /// <c>null</c> if root level, or is not a scope change.
    /// <p/>
    /// If a node has a OpeningMatch, but no <see cref="ClosingMatch"/>,
    /// this indicates a mismatch in scoping tokens: more opening
    /// tokens than than closing tokens.
    /// </summary>
    public ParserMatch? OpeningMatch { get; private set; }

    /// <summary>
    /// ParserMatch that closed the current scope.
    /// <c>null</c> if root level, or is not a scope change.
    /// <p/>
    /// If a node has a ClosingMatch, but no <see cref="OpeningMatch"/>,
    /// this indicates a mismatch in scoping tokens: more closing
    /// tokens than opening tokens.
    /// </summary>
    public ParserMatch? ClosingMatch { get; private set; }

    /// <summary>
    /// Next node in this scope, if any
    /// </summary>
    public ScopeNode? NextNode { get; private set; }

    /// <summary>
    /// Previous node in this scope, if any
    /// </summary>
    public ScopeNode? PrevNode { get; private set; }

    /// <summary>
    /// Nodes within this scope
    /// </summary>
    public List<ScopeNode> Children { get; } = new();

    /// <summary>
    /// Parent node of this scope.
    /// <c>null</c> if root level
    /// </summary>
    public ScopeNode? Parent { get; set; }

    /// <summary>
    /// Add a data node as a child to this node
    /// </summary>
    internal void AddDataFrom(ParserMatch match)
    {
        Link(new ScopeNode
        {
            NodeType = ScopeNodeType.Data,
            DataMatch = match,
            OpeningMatch = null,
            ClosingMatch = null,
            Parent = this
        });
    }

    /// <summary>
    /// Open a new scope, with this node as parent.
    /// <see cref="OpeningMatch"/> of the new node will be '<paramref name="match"/>';
    /// <see cref="ClosingMatch"/> of the new node will be <c>null</c>.
    /// </summary>
    /// <returns>The new scope node</returns>
    internal ScopeNode OpenScope(ParserMatch match)
    {
        var newScope = new ScopeNode
        {
            NodeType = ScopeNodeType.ScopeChange,
            DataMatch = null,
            OpeningMatch = match,
            ClosingMatch = null,
            Parent = this
        };

        Link(newScope);
        return newScope;
    }

    /// <summary>
    /// Close this scope, and return parent.
    /// <see cref="ClosingMatch"/> of this node will be set to '<paramref name="match"/>'.
    /// </summary>
    /// <returns>The parent scope node, or <c>null</c></returns>
    internal ScopeNode? CloseScope(ParserMatch match)
    {
        ClosingMatch = match;
        return Parent;
    }

    /// <summary>
    /// Create a new root node
    /// </summary>
    internal static ScopeNode RootNode()
    {
        return new ScopeNode
        {
            NodeType = ScopeNodeType.Root,
            Parent = null
        };
    }
    
    /// <summary>
    /// Walk the tree, and remove <see cref="ScopeNodeType.Data"/> nodes tagged with <paramref name="generalTag"/>;
    /// If they have a neighboring peer node with the same value and one of the <paramref name="specialTags"/>.
    /// </summary>
    /// <param name="generalTag">Tag to prune</param>
    /// <param name="specialTags">Tags that specialise the general tag</param>
    public void Specialise(string generalTag, params string[] specialTags)
    {
        var nodesToPrune = new List<ScopeNode>();
        DepthFirstWalk(n =>
        {
            if (n.NodeType != ScopeNodeType.Data) return;
            if (n.DataMatch?.Tag != generalTag) return;
            
            // Ok, we have a match on the general tag.
            var value = n.DataMatch.Value;

            // If a neighbor have the same value, and one of the specialised tags, remove the general tag
            if (n.PrevNode?.NodeType == ScopeNodeType.Data
                && n.PrevNode?.DataMatch?.Value == value
                && specialTags.Contains(n.PrevNode?.DataMatch?.Tag))
            {
                nodesToPrune.Add(n);
            }
            else if (n.NextNode?.NodeType == ScopeNodeType.Data
                     && n.NextNode?.DataMatch?.Value == value
                     && specialTags.Contains(n.NextNode?.DataMatch?.Tag))
            {
                nodesToPrune.Add(n);
            }
        });

        foreach (var node in nodesToPrune)
        {
            node.PruneNode();
        }
    }

    /// <summary>
    /// Remove this node and its children from the tree, stitching peers together.
    /// </summary>
    private void PruneNode()
    {
        // Remove from peer links
        if (PrevNode is not null) PrevNode.NextNode = NextNode;
        if (NextNode is not null) NextNode.PrevNode = PrevNode;
        
        // Remove from parent
        if (Parent is not null) Parent.RemoveChild(this);
    }

    /// <summary>
    /// Remove a single node from this node's child list
    /// </summary>
    private void RemoveChild(ScopeNode node)
    {
        Children.Remove(node);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return NodeType switch
        {
            ScopeNodeType.Root => $"Root ({OpeningMatch}, {ClosingMatch}, {Children.Count} children) ",
            ScopeNodeType.Data => DataMatch?.Tag is null ? $"Data ({DataMatch}) " : $"Data {DataMatch?.Tag} ({DataMatch}) ",
            ScopeNodeType.ScopeChange => $"Scope ({OpeningMatch}, {ClosingMatch}, {Children.Count} children) ",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void Link(ScopeNode newChild)
    {
        if (Children.Count > 0)
        {
            var endChild = Children[Children.Count - 1];
            endChild.NextNode = newChild;
            newChild.PrevNode = endChild;
        }

        Children.Add(newChild);
    }

    /// <summary>
    /// Perform an action on every node of the tree, visiting nodes breadth-first.
    /// See also <see cref="DepthFirstWalk"/>
    /// </summary>
    public void BreadthFirstWalk(Action<ScopeNode> action)
    {
        action(this);
        var nextSet = new Queue<ScopeNode>(Children);

        while (nextSet.Count > 0)
        {
            var node = nextSet.Dequeue();
            action(node);
            
            foreach (var child in node.Children) nextSet.Enqueue(child);
        }
    }

    /// <summary>
    /// Perform an action on every node of the tree, visiting nodes depth-first.
    /// See also <see cref="BreadthFirstWalk"/>
    /// </summary>
    public void DepthFirstWalk(Action<ScopeNode> action)
    {
        DepthFirstWalkRec(this, action);
    }

    private static void DepthFirstWalkRec(ScopeNode node, Action<ScopeNode> action)
    {
        action(node);
        foreach (var child in node.Children)
        {
            DepthFirstWalkRec(child, action);
        }
    }
}

/// <summary>
/// Type of node in a <see cref="ScopeNode"/> instance
/// </summary>
public enum ScopeNodeType
{
    /// <summary>
    /// Node is the root of the scoped node tree
    /// </summary>
    Root = 0,

    /// <summary>
    /// Node contains data inside a scope
    /// </summary>
    Data = 1,

    /// <summary>
    /// Node is a scope container
    /// </summary>
    ScopeChange = 2
}