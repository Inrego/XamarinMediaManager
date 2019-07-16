﻿using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using MediaManager.Platforms.Android.Playback;
using MediaManager.Playback;

namespace MediaManager.Platforms.Android.MediaSession
{
    public class MediaBrowserManager
    {
        private TaskCompletionSource<bool> _tcs;

        protected MediaManagerImplementation MediaManager => CrossMediaManager.Android;

        //public MediaControllerCompat MediaController { get; set; }
        protected MediaBrowserCompat MediaBrowser { get; set; }
        protected MediaBrowserConnectionCallback MediaBrowserConnectionCallback { get; set; }
        protected MediaControllerCallback MediaControllerCallback { get; set; }
        protected MediaBrowserSubscriptionCallback MediaBrowserSubscriptionCallback { get; set; }
        protected virtual Java.Lang.Class ServiceType { get; } = Java.Lang.Class.FromType(typeof(MediaBrowserService));

        protected bool IsInitialized { get; private set; } = false;
        protected Context Context => MediaManager.Context;

        public MediaBrowserManager()
        {
        }

        public async Task<bool> Init()
        {
            if (_tcs?.Task != null)
                return await _tcs.Task;

            _tcs = new TaskCompletionSource<bool>();

            if (MediaBrowser == null)
            {
                MediaControllerCallback = new MediaControllerCallback()
                {
                    OnMetadataChangedImpl = metadata =>
                    {
                        //var test = metadata;
                    },
                    OnPlaybackStateChangedImpl = state =>
                    {
                        MediaManager.State = state.ToMediaPlayerState();
                        /*if(MediaManager.State == MediaPlayerState.Stopped)
                        {
                            //TODO: call UnregisterCallback(MediaBrowserSubscriptionCallback) and MediaBrowser.Disconnect() somewhere
                            MediaBrowser.Unsubscribe(MediaBrowser.Root, MediaBrowserSubscriptionCallback);
                            MediaBrowser.Disconnect();
                            MediaController.UnregisterCallback(MediaControllerCallback);
                            MediaController.Dispose();
                            MediaController = null;
                            IsInitialized = false;
                        }*/
                    },
                    OnSessionEventChangedImpl = (string @event, Bundle extras) =>
                    {
                        //Do nothing for now
                    },
                    OnSessionDestroyedImpl = () =>
                    {
                        //Do nothing for now
                    },
                    BinderDiedImpl = () =>
                    {
                        //Do nothing for now
                    },
                    OnSessionReadyImpl = () =>
                    {
                        //Do nothing for now
                    }
                };
                MediaBrowserSubscriptionCallback = new MediaBrowserSubscriptionCallback();

                // Connect a media browser just to get the media session token. There are other ways
                // this can be done, for example by sharing the session token directly.
                //TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                MediaBrowserConnectionCallback = new MediaBrowserConnectionCallback
                {
                    OnConnectedImpl = () =>
                    {
                        var mediaController = MediaManager.MediaController = new MediaControllerCompat(Context, MediaBrowser.SessionToken);
                        mediaController.RegisterCallback(MediaControllerCallback);

                        if (Context is Activity activity)
                            MediaControllerCompat.SetMediaController(activity, mediaController);

                        // Sync existing MediaSession state to the UI.
                        // The first time these events are fired, the metadata and playbackstate are null. 
                        MediaControllerCallback.OnMetadataChanged(mediaController.Metadata);
                        MediaControllerCallback.OnPlaybackStateChanged(mediaController.PlaybackState);

                        MediaBrowser.Subscribe(MediaBrowser.Root, MediaBrowserSubscriptionCallback);

                        IsInitialized = true;
                        _tcs.SetResult(IsInitialized);
                    },
                    OnConnectionFailedImpl = () =>
                    {
                        IsInitialized = false;
                        _tcs.SetResult(IsInitialized);
                    },
                    OnConnectionSuspendedImpl = () =>
                    {
                        IsInitialized = false;
                    }
                };

                MediaBrowser = new MediaBrowserCompat(Context,
                    new ComponentName(
                        Context,
                        ServiceType),
                        MediaBrowserConnectionCallback,
                        null);
            }

            if (!IsInitialized)
            {
                MediaBrowser.Connect();
            }

            return await _tcs.Task;
        }
    }
}
