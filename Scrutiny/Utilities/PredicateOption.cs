using System;

namespace Scrutiny.Utilities
{
    [Flags]
    public enum PredicateOption
    {
        None = 0,
        CaseSensitive = 1, 
        RegularExpression = 2, 
        MultipleTerms = 4
    };
}