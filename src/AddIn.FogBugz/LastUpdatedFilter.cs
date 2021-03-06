﻿namespace Loupe.Extension.FogBugz
{
    /// <summary>
    /// Used to specify how back to check for last update date
    /// </summary>
    public enum LastUpdatedFilter
    {
        /// <summary>
        /// Go back forever
        /// </summary>
        None = 0,

        /// <summary>
        /// One year
        /// </summary>
        OneYear = 1,

        /// <summary>
        /// Three Months
        /// </summary>
        ThreeMonths = 2,

        /// <summary>
        /// One Month
        /// </summary>
        OneMonth = 3,

        /// <summary>
        /// One Week
        /// </summary>
        OneWeek = 4,

        /// <summary>
        /// One Day.  Seriously.
        /// </summary>
        OneDay = 5,
    }
}
