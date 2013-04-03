using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EasyAnimate
{  
    public interface IState {
        //returns true as long as object wants to exist
        bool Update(float pCurrentTime);
        void Complete();
        bool Paused{ get; set; }
        float StartTime { get; set; }
    }
    
    public struct State<T> : IState {
        public delegate void EASetter(T pValue);
        public delegate T EAInterpolate(T pFrom, T pTo, float pQuota);
        public T startValue;
        public T endValue; 
        public float StartTime{ get; set; }
        public float length; //length of animation in seconds
        public Action onCompleted; //action done on complete, does not fire if dissmissed or cleared
        public EAInterpolate interpolator; //function used to interpolate a value over time
        public EASetter setter; //function used to apply the interpolated value    
        /// <summary>
        /// Update is called by the manager
        /// </summary>
        /// <param name="pCurrentTime">current time in seconds</param>
        /// <returns>true if still animating, false if done</returns>
        public bool Update( float pCurrentTime ){
            if (StartTime > pCurrentTime) { //before the animation starts
                return true;
            }
            if ((StartTime + length) <= pCurrentTime) { //the animation has ended
                return false;
            }
            else { //while animating
                if (setter != null) {
                    setter(interpolator(startValue, endValue, (pCurrentTime - StartTime) / length));
                }
                return true;
            }
        }
        public void Complete() {
            if (setter != null) { setter(endValue); }
            if (onCompleted != null) {
                onCompleted();
            }
        }
        public bool Paused {
            set;
            get;
        }
    }
    
    //static Dictionary<object, Dictionary<string, IState>> _animStates = new Dictionary<object, Dictionary<string, IState>>();
    Dictionary<int, IState> _animStates = new Dictionary<int, IState>();
    IState _soloState = null;

    /// <summary>
    /// Add a new animation state to the manager only if no animation with the same object and channel is not playing
    /// </summary>
    
    /// <param name="pChannel">a name token</param>
    /// <param name="pState">an EasyAnimate.State object</param>
    public void SetIfVacant(string pChannel, IState pState) {
        if (!_animStates.ContainsKey(GetHash(pChannel)))
            _animStates[GetHash(pChannel)] = pState;
    }
    /// <summary>
    /// Add a new animation state to the manager
    /// </summary>
    
    /// <param name="pChannel">a name token</param>
    /// <param name="pState">an EasyAnimate.State object</param>
    public void Set(string pChannel, IState pState) {
        
        _animStates[GetHash(pChannel)] =  pState;

    }

    public T Get<T>(string pChannel) where T : IState {
        int hash = GetHash( pChannel);
        IState result;
        if (_animStates.TryGetValue(hash, out result)) {
            if (result is T) {
                return (T)result;
            }
            else {
                throw new InvalidCastException("could not cast object of type " + result.GetType().Name + " to " + typeof(T).Name);
            }
        }
        else {
            return default(T);
        }
    }

    internal void Update(){
        foreach (KeyValuePair<int, IState> kv in _animStates.ToArray()) {
            int key = kv.Key;
            IState e = kv.Value;
            if (e.Paused) { 
                e.StartTime += Time.deltaTime;
            }
            if (!e.Update(Time.time)) {
                _animStates.Remove(key);
                if (_soloState == e) {
                    UnpauseAll();
                    _soloState = null;
                }
                e.Complete(); //must be called after key is removed in case the complete event triggers a reuse of the key.
            }
        }
    }

    /// <summary>
    /// tries to stop an animation
    /// </summary>
    /// <returns>true if an animation was stopped</returns>
    public bool Stop(string pChannel) { 
        int hash = GetHash(pChannel);
        if (_animStates.ContainsKey(hash)) {
            _animStates.Remove(hash);
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
        int hash = GetHash(pChannel);
        EasyAnimate.IState output;
        if (_animStates.TryGetValue(hash, out output)) {
            output.Complete();
            _animStates.Remove(hash);
            return true;
        }
        else {
            return false;
        }
    }
    public void UnpauseAll() {
        foreach (var e in _animStates.Values) {
            e.Paused = false;
        }
    }
    
    public void Solo(string pKey) {
        foreach (var e in _animStates.Values) {
            e.Paused = true;
        }
        _soloState = Get<IState>(pKey);
        _soloState.Paused = false;
    }

    internal void Clear() {
        _animStates.Clear();
    }

    private int GetHash( string pChannel)
    {
        return pChannel.GetHashCode();
    }


    internal bool HasState( string pChannel) {
        int hash = GetHash(pChannel);
        if (_animStates.ContainsKey(hash)) {
            return true;
        }
        return false;
    }
}
