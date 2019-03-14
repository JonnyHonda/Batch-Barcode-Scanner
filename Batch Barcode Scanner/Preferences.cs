using System;
using Android.Content;
using Android.Preferences;

namespace Batch_Barcode_Scanner
{
    /// <summary>
    /// App preferences. This call allows the application to save prefernces with the applications context rather than in a database,
    /// These prefs are lost when the application is uninstalled
    /// These preferences are key/value pairs and can be retrieved from anywhere within the application
    /// </summary>
    public class AppPreferences
    {
        private ISharedPreferences sharedPreferences;
        private ISharedPreferencesEditor preferencesEditor;
        private readonly Context applicationContext;

        //private static String PREFERENCE_ACCESS_KEY = "PREFERENCE_ACCESS_KEY";

        public AppPreferences(Context context)
        {
            this.applicationContext = context;
            sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(applicationContext);
            preferencesEditor = sharedPreferences.Edit();
        }

        /// <summary>
        /// Saves the access key.
        /// </summary>
        /// <param name="preferenceAccessKey">Preference access key.</param>
        /// <param name="value">Value.</param>
        /// <param name="is_mandatory">If set to <c>true</c> is mandatory.</param>
        public void SaveAccessKey(String preferenceAccessKey,string value, bool is_mandatory = false)
        {
            if (string.IsNullOrEmpty(value) && is_mandatory == true)
            {
                throw new ArgumentException(preferenceAccessKey + " cannot be null or empty string", nameof(value));
            }
            else
            {
                preferencesEditor.PutString(preferenceAccessKey, value);
                preferencesEditor.Commit();

            }

        }

        /// <summary>
        /// Gets the access key.
        /// </summary>
        /// <returns>The access key.</returns>
        /// <param name="preferenceAccessKey">Preference access key.</param>
        public string GetAccessKey(String preferenceAccessKey)
        {
            return sharedPreferences.GetString(preferenceAccessKey, "");
        }

        /// <summary>
        /// Clears the prefs.
        /// </summary>
        public void ClearPrefs(){
            preferencesEditor.Clear().Commit(); 
        }

    }

}

