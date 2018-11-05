using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobExpressions
{
    internal class Scanner
    {
        private readonly string _source;

        private int _sourceIndex;
        private int _currentCharacter;
        private int _startIndex;
        private TokenKind _currentKind;
        private readonly StringBuilder _spelling;

        public Scanner(string source)
        {
            this._source = source;
            this._sourceIndex = 0;
            this._startIndex = 0;
            _spelling = new StringBuilder();
            SetCurrentCharacter();
        }

        public void TakeIt()
        {
            _spelling.Append((char)_currentCharacter);

            SkipIt();
        }

        public void SkipIt()
        {
            this._sourceIndex++;

            SetCurrentCharacter();
        }

        private void SetCurrentCharacter()
        {
            if (this._sourceIndex >= this._source.Length)
                this._currentCharacter = -1;
            else
                this._currentCharacter = this._source[this._sourceIndex];
        }

        private int PeekChar()
        {
            var sourceIndex = this._sourceIndex + 1;
            if (sourceIndex >= this._source.Length)
                return -1;

            return this._source[sourceIndex];
        }

        public Token Scan()
        {
            this._currentKind = this.ScanToken();

            var token = new Token(_currentKind, _spelling.ToString());
            _spelling.Clear();

            _startIndex = _sourceIndex;

            return token;
        }

        private TokenKind ScanToken()
        {
            if (this._currentCharacter == -1)
                return TokenKind.EOT;

            var current = (char)_currentCharacter;

            if (char.IsLetter(current) && _sourceIndex == 0 && this.PeekChar() == ':')
            {
                TakeIt(); // letter
                TakeIt(); // :
                return TokenKind.WindowsRoot;
            }

            if (IsIdentifierCharacter(current))
            {
                return TakeIdentifier();
            }

            switch (this._currentCharacter)
            {
                case '*':
                    this.TakeIt();
                    if (this._currentCharacter == '*')
                    {
                        this.TakeIt();
                        return TokenKind.DirectoryWildcard;
                    }

                    return TokenKind.Wildcard;

                case '\\':
                    this.TakeIt();
                    switch (this._currentCharacter)
                    {
                        case '*':
                        case '?':
                        case '{':
                        case '}':
                        case '[':
                        case ']':
                        case ' ':
                            this.TakeIt(); // escaped char
                            return TokenKind.EscapeSequence;

                        default:
                            return TokenKind.Identifier;
                    }

                case '?':
                    this.TakeIt();
                    return TokenKind.CharacterWildcard;

                case '!':
                    this.TakeIt();
                    return TokenKind.CharacterSetInvert;

                case '[':
                    this.TakeIt();
                    return TokenKind.CharacterSetStart;

                case ']':
                    this.TakeIt();
                    return TokenKind.CharacterSetEnd;

                case '{':
                    this.TakeIt();
                    return TokenKind.LiteralSetStart;

                case ',':
                    this.TakeIt();
                    return TokenKind.LiteralSetSeperator;

                case '}':
                    this.TakeIt();
                    return TokenKind.LiteralSetEnd;

                case '/':
                    this.TakeIt();
                    return TokenKind.PathSeparator;

                default:
                    throw new Exception("Unable to scan for next token. Stuck on '" + (char)this._currentCharacter + "'");
            }
        }

        private TokenKind TakeIdentifier()
        {
            while (true)
            {
                var c = (char)this._currentCharacter;
                if (IsIdentifierCharacter(c))
                {
                    this.TakeIt();
                    continue;
                }

                break;
            }

            return TokenKind.Identifier;
        }

        private static bool IsIdentifierCharacter(char c) => char.IsLetterOrDigit(c) || " .-~_$".Contains(c);
    }
}
