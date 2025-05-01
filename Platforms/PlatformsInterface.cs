using System;

namespace Calctus.Interfaces
{
    /// <summary>
    /// Defines a listener for clipboard change events.
    /// </summary>
    public interface IClipboardListener : IDisposable
    {
        /// <summary>
        /// Occurs when the clipboard content has changed.
        /// </summary>
        event EventHandler ClipboardChanged;

        /// <summary>
        /// Forces a refresh of the current clipboard content state.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// Defines methods to register or unregister the application at system startup.
    /// </summary>
    public interface IStartupRegistrar
    {
        /// <summary>
        /// Determines whether the application is registered to start on login.
        /// </summary>
        /// <returns>True if registered; otherwise, false.</returns>
        bool IsRegistered();

        /// <summary>
        /// Registers or unregisters the application for startup.
        /// </summary>
        /// <param name="enable">True to register; false to unregister.</param>
        void SetRegistration(bool enable);
    }
}
