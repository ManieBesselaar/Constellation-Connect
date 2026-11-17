namespace Constellation.Localization
{
    using UnityEngine.Localization.Settings;

    /// <summary>
    /// Reads a key from the <c>ConstellationStrings</c> table, falling back to the value authored
    /// on the ConstellationSO when localization is not initialised or the key is missing.
    /// </summary>
    public static class LocalizedText
    {
        /// <summary>Name of the string table built by the localization setup tool.</summary>
        public const string TableName = "ConstellationStrings";

        /// <summary>
        /// Resolves a localized string, never throwing and never returning an empty result.
        /// </summary>
        /// <param name="key">Table entry key, e.g. THEME_FAITH_STRENGTH.</param>
        /// <param name="fallback">Value returned when the table cannot supply the key.</param>
        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;

            try
            {
                if (!LocalizationSettings.HasSettings) return fallback;

                var database = LocalizationSettings.StringDatabase;
                if (database == null) return fallback;

                string value = database.GetLocalizedString(TableName, key, null, FallbackBehavior.UseProjectSettings);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch
            {
                // Localization may not have finished initialising on the very first frame.
                return fallback;
            }
        }
    }
}
