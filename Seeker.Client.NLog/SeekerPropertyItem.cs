using NLog.Config;
using NLog.Layouts;

namespace Seeker.Client.NLog
{
    /// <summary>
    /// Configures a property that enriches events sent to Seeker.
    /// </summary>
    [NLogConfigurationItem]
    public class SeekerPropertyItem
    {
        #region Properties

        /// <summary>
        /// The name of the property.
        /// </summary>
        [RequiredParameter]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The value of the property.
        /// </summary>
        [RequiredParameter]
        public Layout Value
        {
            get;
            set;
        }

        #endregion
    }
}
