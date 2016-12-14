namespace NServiceBus
{
    using System;

    sealed class ReleaseDateAttribute : Attribute
    {
        public ReleaseDateAttribute()
        {
            OriginalDate = GitVersionInformation.CommitDate;
            Date = GitVersionInformation.CommitDate;
        }

        public string OriginalDate { get; }
        public string Date { get; }
    }
}