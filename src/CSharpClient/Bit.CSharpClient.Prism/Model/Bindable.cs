﻿using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.Model
{
    public interface ITrackable
    {
        void BeginTrack();

        void AcceptChanges();

        void RevertChanges();

        bool HasChanges { get; }
    }

    public class Bindable : BindableBase, ITrackable
    {
        public virtual void RaiseMemberChanged([CallerMemberName] string memberName = null)
        {
            RaisePropertyChanged(memberName);
        }

        public virtual void RaiseThisChanged()
        {
            RaisePropertyChanged(".");
        }

        private Dictionary<string, object> changedProps = null;

        private bool IsBeingTracked => changedProps != null;

        public virtual void OnPropertyChanged(string propertyName, object before, object after)
        {
            if (IsBeingTracked)
            {
                if (!changedProps.Any(p => p.Key == propertyName))
                    changedProps.Add(propertyName, before);
                else if (changedProps.Any(p => p.Key == propertyName && p.Value == after))
                    changedProps.Remove(propertyName);
            }
        }

        void ITrackable.BeginTrack()
        {
            if (IsBeingTracked)
                throw new InvalidOperationException("Object is being tracked");

            changedProps = new Dictionary<string, object> { };
        }

        void ITrackable.AcceptChanges()
        {
            if (!IsBeingTracked)
                throw new InvalidOperationException("Object isn't being tracked");

            changedProps.Clear();
        }

        void ITrackable.RevertChanges()
        {
            if (!IsBeingTracked)
                throw new InvalidOperationException("Object isn't being tracked");

            Dictionary<string, object> localProps = changedProps;

            try
            {
                changedProps = null; // To prevent calling OnPropertyChanged with IsBeingTracked value true.
                foreach (KeyValuePair<string, object> prp in localProps)
                {
                    PropertyInfo propInfo = GetType().GetProperty(prp.Key);

                    if (propInfo == null)
                        throw new InvalidOperationException($"Property {prp.Key} could not be found");

                    if (propInfo.CanWrite)
                        propInfo.SetValue(this, prp.Value);
                }
            }
            finally
            {
                changedProps = new Dictionary<string, object> { };
            }
        }

        bool ITrackable.HasChanges => IsBeingTracked && changedProps.Any();
    }
}
