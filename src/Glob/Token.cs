using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobExpressions
{
    internal struct Token
    {
        public Token(TokenKind kind, string spelling)
        {
            this.Kind = kind;
            Spelling = spelling;
        }

        public TokenKind Kind { get; }
        public string Spelling { get; }

        public override string ToString()
        {
            return Kind + ": " + Spelling;
        }
    }
}
