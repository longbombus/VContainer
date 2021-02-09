using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VContainer.Unity
{
    public sealed class VContainerParentTypeReferenceNotFound : Exception
    {
        public readonly Type ParentType;

        public VContainerParentTypeReferenceNotFound(Type parentType, string message)
            : base(message)
        {
            ParentType = parentType;
        }
    }


    public partial class LifetimeScope
    {
        static readonly List<(LifetimeScope, VContainerParentTypeReferenceNotFound)> WaitingList =
            new List<(LifetimeScope, VContainerParentTypeReferenceNotFound)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SubscribeSceneEvents()
        {
            SceneManager.sceneLoaded -= ReleaseWaitingList;
            SceneManager.sceneLoaded += ReleaseWaitingList;
        }

        static void WaitForAwake(LifetimeScope lifetimeScope, VContainerParentTypeReferenceNotFound ex)
        {
            WaitingList.Add((lifetimeScope, ex));
        }

        static void CancelWaiting(LifetimeScope lifetimeScope)
        {
            for (var i = WaitingList.Count - 1; i >= 0; i--)
            {
                if (WaitingList[i].Item1 == lifetimeScope)
                {
                    WaitingList.RemoveAt(i);
                }
            }
        }

        static void ReleaseWaitingList(Scene scene, LoadSceneMode mode)
        {
            for (var i = WaitingList.Count - 1; i >= 0; i--)
            {
                var (waiting, ex) = WaitingList[i];
                if (waiting.gameObject.scene == scene)
                {
                    WaitingList.RemoveAt(i);
                    waiting.Awake(); // Re-throw if parent not found.
                }
            }
        }

        static void ReleaseWaitingListFrom(LifetimeScope awakedParent)
        {
            if (WaitingList.Count < 0) return;

            var type = awakedParent.GetType();

            for (var i = WaitingList.Count - 1; i >= 0; i--)
            {
                var (waiting, ex) = WaitingList[i];
                if (ex.ParentType == type)
                {
                    waiting.parentReference.Object = awakedParent;
                    WaitingList.RemoveAt(i);
                    waiting.Awake();
                }
            }
        }
    }
}