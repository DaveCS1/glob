namespace GlobExpressions
{
    internal enum TokenKind
    {
        Wildcard = 0,              // *
        CharacterWildcard = 1,     // ?
        DirectoryWildcard = 2,     // **

        CharacterSetStart = 3,     // [
        CharacterSetEnd = 4,       // ]
        CharacterSetInvert = 1,    // !

        LiteralSetStart = 5,       // {
        LiteralSetSeperator = 6,   // ,
        LiteralSetEnd = 7,         // }

        PathSeparator = 8,         // /
        EscapeSequence = 9,        // \

        Identifier = 10,           // Letter or Number
        WindowsRoot = 11,          // :

        EOT = 100,
    }
}
