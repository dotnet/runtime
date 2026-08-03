namespace System.Private.CoreLib.src.System.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="DateOnly"/>
        /// </summary>
        /// <returns><see cref="DateOnly"/></returns>
        public DateOnly ToDateOnly() =>
            DateOnly.FromDateTime(dateTime);

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="TimeOnly"/>
        /// </summary>
        /// <returns><see cref="TimeOnly"/></returns>
        public TimeOnly ToTimeOnly() =>
            TimeOnly.FromDateTime(dateTime);
    }

    extension(DateTime? dateTime)
    {
        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="DateOnly"/>
        /// </summary>
        /// <returns><see cref="DateOnly"/></returns>
        public DateOnly ToDateOnly() =>
            DateOnly.FromDateTime(dateTime.Value);

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="TimeOnly"/>
        /// </summary>
        /// <returns><see cref="TimeOnly"/></returns>
        public TimeOnly ToTimeOnly() =>
            TimeOnly.FromDateTime(dateTime.Value);

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="DateOnly"/>
        /// </summary>
        /// <returns><see cref="DateOnly"/> is <paramref name="dateTime"/> is null returns null</returns>
        public DateOnly? ToDateOnlyOrNull() =>
            dateTime is not null
                ? dateTime.Value.ToDateOnly()
                : null;

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="TimeOnly"/>
        /// </summary>
        /// <returns><see cref="TimeOnly"/> is <paramref name="dateTime"/> is null returns null</returns>
        public TimeOnly? ToTimeOnlyOrNull() =>
            dateTime is not null
                ? dateTime.Value.ToTimeOnly()
                : null;

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="DateOnly"/>
        /// </summary>
        /// <returns><see cref="DateOnly"/> is <paramref name="dateTime"/> is null returns default value</returns>
        public DateOnly ToDateOnlyOrDefault() =>
            dateTime is not null
                ? dateTime.Value.ToDateOnly()
                : default;

        /// <summary>
        /// Converts <see cref="DateTime"/> to <see cref="TimeOnly"/>
        /// </summary>
        /// <returns><see cref="TimeOnly"/> is <paramref name="dateTime"/> is null returns default value</returns>
        public TimeOnly ToTimeOnlyOrDefaultrg() =>
            dateTime is not null
                ? dateTime.Value.ToTimeOnly()
                : default;
    }
}
