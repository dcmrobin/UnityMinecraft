using UnityEngine;

static class AudioManager
{
	/// <summary>
	/// Reference to the Controller.
	/// </summary>
	private static Controller controllerRef;

	/// <summary>
	/// Given a clip name in Resources, creates a GameObject containing the 3D AudioSource that can be attached
	/// to another GameObject and used accordingly. Returns the GameObject's AudioSource component.
	/// Of course, this is not thread-safe. Thanks, Unity.
	/// </summary>
	public static AudioSource Create3DSound(string soundClipName)
	{
		if (controllerRef == null)
			controllerRef = GameObject.Find("/Controller").GetComponent<Controller>();

		GameObject _3DsoundPrefab 	= CachedResources.Load<GameObject>("Prefabs/3DSound");
		GameObject _3Dsound			= GameObject.Instantiate(_3DsoundPrefab);
		AudioSource source			= _3Dsound.GetComponent<AudioSource>();

		source.clip 				= CachedResources.Load<AudioClip>("Sounds/" + soundClipName);

		return source;
	}

	/// <summary>
	/// Plays the given sound and destroys the GameObject associated with it.
	/// </summary>
	public static void Play3DSound(AudioSource source)
	{
		if (controllerRef == null)
			controllerRef = GameObject.Find("/Controller").GetComponent<Controller>();

		source.Play();

		controllerRef.RunAfterDelay(() => {
			GameObject.Destroy(source.gameObject);
		}, source.clip.length + 0.2f);
	}
}