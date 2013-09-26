using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EasyAnimate
{  
	public struct Channel{
		public IState state;
		public string name;
	}
	public interface IState {
		//returns true as long as object wants to exist
		bool Update(float pCurrentTime);
		void Complete();
		bool Paused{ get; set; }
		float StartTime { get; set; }
	}

	public struct State<T> : IState {
		public delegate void EASetter(T pValue);

			public delegate T EAInterpolate(T pFrom,T pTo,float pQuota);

			public T startValue;
			public T endValue;

			public float StartTime{ get; set; }

			public float duration;
			//length of animation in seconds
			public Action onCompleted;
			//action done on complete, does not fire if dissmissed or cleared
			public EAInterpolate interpolator;
			//function used to interpolate a value over time
			public EASetter setter;
			//function used to apply the interpolated value    
			/// <summary>
			/// Update is called by the manager
			/// </summary>
			/// <param name="pCurrentTime">current time in seconds</param>
			/// <returns>true if still animating, false if done</returns>
			public bool Update(float pCurrentTime)
			{
				if (StartTime > pCurrentTime)
				{ //before the animation starts
					return true;
				}
				if ((StartTime + duration) <= pCurrentTime)
				{ //the animation has ended
					return false;
				}
				else
				{ //while animating
					if (setter != null)
					{
						setter(interpolator(startValue, endValue, (pCurrentTime - StartTime) / duration));
					}
					return true;
				}
			}

			public void Complete()
			{
				if (setter != null)
				{
					setter(endValue);
				}
				if (onCompleted != null)
				{
					onCompleted();
				}
			}

			public bool Paused
			{
				set;
				get;
			}
		}
		//static Dictionary<object, Dictionary<string, IState>> _animStates = new Dictionary<object, Dictionary<string, IState>>();
		List<Channel> _animStates = new List<Channel>();
		IState _soloState = null;

		/// <summary>
		/// Add a new animation state to the manager only if no animation with the same object and channel is not playing
		/// </summary>

		/// <param name="pChannel">a name token</param>
		/// <param name="pState">an EasyAnimate.State object</param>
		public void SetIfVacant(string pChannel, IState pState)
		{
			var newChan = new Channel()
			{
				name = pChannel,
				state = pState
			};
			int channelIndex = _animStates.FindIndex(p => p.name == pChannel);
			if (channelIndex == -1)
			{
				_animStates.Add(newChan);
			}
		}

		/// <summary>
		/// Add a new animation state to the manager
		/// </summary>

		/// <param name="pChannel">a name token</param>
		/// <param name="pState">an EasyAnimate.State object</param>
		public void Set(string pChannel, IState pState)
		{
			var newChan = new Channel()
			{
				name = pChannel,
				state = pState
			};
			int channelIndex = _animStates.FindIndex(p => p.name == pChannel);
			if (channelIndex == -1)
			{
				_animStates.Add(newChan);
			}
			else
			{
				_animStates[channelIndex] = newChan;
			}
		}

		public T Get<T>(string pChannel) where T : IState
		{
			int channelIndex = _animStates.FindIndex(p => p.name == pChannel);
			if (channelIndex == -1)
			{
				return default(T);
			}
			else
			{
				var result = _animStates[channelIndex].state;
				if (result is T)
				{
					return (T)result;
				}
				else
				{
					throw new InvalidCastException("could not cast object of type " + result.GetType().Name + " to " + typeof(T).Name);
				}

			}
		}
	bool firstUpdate = true;
	float _lastUpdateTime = float.MinValue;
	internal void Update()
	{
		if (firstUpdate)
		{
			_lastUpdateTime = Time.time;
			firstUpdate = false;
		}
		float deltaTime = Time.time - _lastUpdateTime;
		for( int i = _animStates.Count -1 ; i >= 0; i--){
			var state = _animStates[i].state;
			if (state.Paused) { 
				state.StartTime += deltaTime;
			}
			if (!state.Update(Time.time)) {
				_animStates.RemoveAt(i);
				if (_soloState == state) {
					UnpauseAll();
					_soloState = null;
				}
				state.Complete(); //must be called after key is removed in case the complete event triggers a reuse of the key.
			}
		}
		_lastUpdateTime = Time.time;
	}

	/// <summary>
	/// tries to stop an animation
	/// </summary>
	/// <returns>true if an animation was stopped</returns>
	public bool Stop(string pChannel) { 
		int channelIndex = _animStates.FindIndex(p => p.name == pChannel);
		if (channelIndex != -1) {
			_animStates.RemoveAt(channelIndex);
			return true;
		}
		else {
			return false;
		}
	}

	/// <summary>
	/// tries to complete an animation
	/// </summary>
	/// <returns>true if an animation was stopped</returns>
	public bool Complete(string pChannel) {
		int channelIndex = _animStates.FindIndex(p => p.name == pChannel);
		if (channelIndex != -1){
			var state = _animStates[channelIndex].state;
			_animStates.RemoveAt(channelIndex);
			state.Complete();
			return true;
		}
		return false;
	}

	public void CompleteAll() {
		_animStates.ForEach(p => {
			p.state.Complete();
		});
		_animStates.Clear();
	}

	public void UnpauseAll() {
		Update();
		_animStates.ForEach(p => p.state.Paused = false);
	}

	public void PauseAll()
	{
		_animStates.ForEach(p => p.state.Paused = true);
	}

	public void Solo(string pKey) {
		var _soloState = Get<IState>(pKey);
		if (_soloState != null)
		{
			_animStates.ForEach(p => p.state.Paused = true);
			_soloState.Paused = false;
		}
		else
		{
			UnpauseAll();
		}
	}

	public bool IsAnimating()
	{
		return _animStates.Count > 0;
	}

	internal void Clear() {
		_animStates.Clear();
	}
	internal bool HasState( string pChannel) {
		return _animStates.FindIndex(p => p.name == pChannel) != -1;
	}
	public EasyAnimate(){

	}
}
