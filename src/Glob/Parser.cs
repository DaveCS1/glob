﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlobExpressions.AST;

namespace GlobExpressions
{
    internal class Parser
    {
        private readonly Scanner _scanner;
        private Token _currentToken;

        public Parser(string pattern = null)
        {
            this._scanner = new Scanner(pattern ?? "");
            this._currentToken = _scanner.Scan();
        }

        private void Accept(TokenKind expectedKind)
        {
            if (this._currentToken.Kind == expectedKind)
            {
                this.AcceptIt();
                return;
            }

            throw new GlobPatternException($"Expected {expectedKind}, Got {this._currentToken.Kind}.");
        }

        private void AcceptIt()
        {
            if (this._scanner == null)
            {
                throw new GlobPatternException("No pattern was provided");
            }
            this._currentToken = this._scanner.Scan();
        }

        private Identifier ParseIdentifier(bool inLiteralSet)
        {
            bool IdentToken() => this._currentToken.Kind == TokenKind.Identifier ||
                                 this._currentToken.Kind == TokenKind.EscapeSequence ||
            (!inLiteralSet && _currentToken.Kind == TokenKind.LiteralSetSeperator);

            var sb = new StringBuilder();
            if (!IdentToken())
                throw new GlobPatternException("Unable to parse Identifier");

            sb.Append(_currentToken.Kind == TokenKind.EscapeSequence
                ? this._currentToken.Spelling.Substring(1)
                : this._currentToken.Spelling);
            this.AcceptIt();

            while (IdentToken())
            {
                sb.Append(_currentToken.Kind == TokenKind.EscapeSequence
                    ? this._currentToken.Spelling.Substring(1)
                    : this._currentToken.Spelling);

                this.AcceptIt();
            }

            return new Identifier(sb.ToString());
        }

        private LiteralSet ParseLiteralSet()
        {
            var items = new List<Identifier>();
            this.Accept(TokenKind.LiteralSetStart);
            items.Add(this.ParseIdentifier(true));

            while (this._currentToken.Kind == TokenKind.LiteralSetSeperator)
            {
                this.AcceptIt();
                items.Add(this.ParseIdentifier(true));
            }
            this.Accept(TokenKind.LiteralSetEnd);
            return new LiteralSet(items);
        }

        private CharacterSet ParseCharacterSet()
        {
            this.Accept(TokenKind.CharacterSetStart);
            var inverted = false;
            if (this._currentToken.Kind == TokenKind.CharacterSetInvert)
            {
                this.AcceptIt();
                inverted = true;
            }
            var sb = new StringBuilder();
            // first token is special and we allow more things like ] or [ at the beginning
            switch (_currentToken.Kind)
            {
                case TokenKind.CharacterWildcard:
                case TokenKind.Wildcard:
                case TokenKind.CharacterSetStart:
                case TokenKind.CharacterSetEnd:
                case TokenKind.Identifier:
                case TokenKind.EscapeSequence:
                case TokenKind.PathSeparator:
                    sb.Append(_currentToken.Spelling);
                    AcceptIt();
                    break;

                default:
                    throw new GlobPatternException(
                        "Unable to parse SubSegment. " +
                        "   Expected one of CharacterWildcard | Wildcard | CharacterSetStart | CharacterSetEnd | Identifier. " +
                        $"Found: {this._currentToken.Kind}"
                    );
            }

            while (_currentToken.Kind != TokenKind.CharacterSetEnd)
            {
                switch (_currentToken.Kind)
                {
                    case TokenKind.CharacterWildcard:
                    case TokenKind.Wildcard:
                    case TokenKind.Identifier:
                    case TokenKind.EscapeSequence:
                    case TokenKind.PathSeparator:
                        sb.Append(_currentToken.Spelling);
                        AcceptIt();
                        break;

                    default:
                        throw new GlobPatternException(
                            "Unable to parse SubSegment. " +
                            "   Expected one of CharacterWildcard | Wildcard | CharacterSetStart | CharacterSetEnd | Identifier. " +
                            $"Found: {this._currentToken.Kind}"
                        );
                }
            }

            this.Accept(TokenKind.CharacterSetEnd);
            return new CharacterSet(sb.ToString(), inverted);
        }

        private StringWildcard ParseWildcard()
        {
            this.Accept(TokenKind.Wildcard);
            return StringWildcard.Default;
        }

        private CharacterWildcard ParseCharacterWildcard()
        {
            this.Accept(TokenKind.CharacterWildcard);
            return CharacterWildcard.Default;
        }

        // SubSegment := Identifier | CharacterSet | LiteralSet | CharacterWildcard | Wildcard
        private SubSegment ParseSubSegment()
        {
            switch (this._currentToken.Kind)
            {
                case TokenKind.EscapeSequence:
                case TokenKind.Identifier:
                    return this.ParseIdentifier(false);

                case TokenKind.CharacterSetStart:
                    return this.ParseCharacterSet();

                case TokenKind.LiteralSetStart:
                    return this.ParseLiteralSet();

                case TokenKind.CharacterWildcard:
                    return this.ParseCharacterWildcard();

                case TokenKind.Wildcard:
                    return this.ParseWildcard();

                default:
                    throw new GlobPatternException(
                        "Unable to parse SubSegment. " +
                        "   Expected one of Identifier | CharacterSet | LiteralSet | CharacterWildcard | Wildcard. " +
                        $"Found: {this._currentToken.Kind}"
                    );
            }
        }

        // Segment := DirectoryWildcard | DirectorySegment
        // DirectorySegment := SubSegment SubSegment*
        private Segment ParseSegment()
        {
            /** NOTE: DirectoryWildcard should normally take a whole segment, but in the case
             * of a DirectoryWildcard being combined with a SubSegment we will convert the
             * DirectoryWildcard to a normal Wildcard */
            var items = new List<SubSegment>();
            var isDirectoryWildcard = false;

            while (true)
            {
                switch (this._currentToken.Kind)
                {
                    case TokenKind.Identifier:
                    case TokenKind.CharacterSetStart:
                    case TokenKind.LiteralSetStart:
                    case TokenKind.EscapeSequence:
                    case TokenKind.CharacterWildcard:
                    case TokenKind.Wildcard:
                        items.Add(this.ParseSubSegment());
                        continue;

                    case TokenKind.DirectoryWildcard:
                        // treat DirectoryWildcard as StringWildcard if we have more than one SubSegment
                        isDirectoryWildcard = true;
                        this.AcceptIt();
                        items.Add(StringWildcard.Default);
                        continue;
                }
                break;
            }

            if (items.Count == 1 && isDirectoryWildcard)
                return DirectoryWildcard.Default;

            return new DirectorySegment(items);
        }

        private Root ParseRoot()
        {
            if (this._currentToken.Kind == TokenKind.PathSeparator)
                return new Root(); // don't eat it so we can leave it for the segments

            if (this._currentToken.Kind == TokenKind.WindowsRoot)
            {
                var root = new Root(this._currentToken.Spelling);
                this.Accept(TokenKind.WindowsRoot);
                return root;
            }

            return new Root();
        }

        // Tree := ( Root | Segment ) ( '/' Segment )*
        protected internal Tree ParseTree()
        {
            var items = new List<Segment>();

            switch (this._currentToken.Kind)
            {
                case TokenKind.EOT:
                    break;

                case TokenKind.PathSeparator:
                case TokenKind.WindowsRoot:
                    items.Add(this.ParseRoot());
                    break;

                default:
                    items.Add(this.ParseSegment());
                    break;
            }

            while (this._currentToken.Kind == TokenKind.PathSeparator)
            {
                this.AcceptIt();
                items.Add(this.ParseSegment());
            }

            if (_currentToken.Kind != TokenKind.EOT)
                items.Add(this.ParseSegment());

            return new Tree(items);
        }

        public GlobNode Parse()
        {
            Tree path;

            switch (this._currentToken.Kind)
            {
                case TokenKind.WindowsRoot:
                case TokenKind.PathSeparator:
                case TokenKind.Identifier:
                case TokenKind.EscapeSequence:
                case TokenKind.CharacterSetStart:
                case TokenKind.LiteralSetStart:
                case TokenKind.CharacterWildcard:
                case TokenKind.Wildcard:
                case TokenKind.DirectoryWildcard:
                case TokenKind.EOT:
                    path = this.ParseTree();
                    break;

                default:
                    throw new GlobPatternException("Expected Tree, found: " + _currentToken.Kind);
            }

            this.Accept(TokenKind.EOT);
            return path;
        }
    }
}
