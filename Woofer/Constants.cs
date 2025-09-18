namespace Woofer;

public static class Constants
{
    /// <summary>The application ID that is used to identify your application,
    /// see https://developer.gnome.org/documentation/tutorials/application-id.html.
    /// This should be automatically replaced when the application is created.
    /// </summary>
    public const string APP_ID = "woofer.tenderowl.com";

    /// <summary>
    /// A shorter name for the application.
    /// This is case sensitive and should not contain spaces.
    /// This should be automatically replaced when the application is created.
    /// </summary>
    public const string APP_SHORT_NAME = "Woofer";

    /// <summary>
    /// The display name of the application.
    /// This should be automatically replaced when the application is created.
    /// </summary>
    public const string APP_DISPLAY_NAME = "Woofer";

    /// <summary>
    /// The display name of the application.
    /// This should be automatically replaced when the application is created.
    /// </summary>
    public const string RESOURCES_PATH = "/app/share/Woofer/woofer.tenderowl.com.gresource";

    public static readonly string[] SUPPORTED_FORMATS = {
        ".mp3", ".ogg", ".flac", ".m4a", ".aac", ".wav"
    };
    public static readonly string[] COVER_FILENAMES = {
            "folder.jpg",
    "folder.jpeg",
    "folder.png",
    "cover.jpg",
    "cover.jpeg",
    "cover.png",
    "album.jpg",
    "album.jpeg",
    "album.png",
    "front.jpg",
    "front.jpeg",
    "front.png",
     };
}