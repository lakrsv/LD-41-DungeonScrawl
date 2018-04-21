﻿// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SpellHint.cs" author="Lars" company="None">
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using System;
    using System.Collections.Generic;

    using DG.Tweening;

    using TMPro;

    using UnityEngine;

    using Utilities.Game.ObjectPool;

    public class SpellHint : MonoBehaviour, IPoolable
    {
        private readonly List<Tween> _currentTweens = new List<Tween>();

        private readonly Vector2 _followOffset = new Vector2(0f, 0.25f);

        [SerializeField]
        private float _appearDuration = 0.75f;

        [SerializeField]
        private float _wordCompleteAnimationDuration = 0.25f;

        private Canvas _canvas;

        private bool _follow;

        private Transform _followTarget;

        [SerializeField]
        private float _movementSpeed = 2.5f;

        [SerializeField]
        private TextMeshProUGUI _text;

        public bool IsEnabled { get; private set; }

        public bool WordWasInvalid { get; private set; }

        public bool WordComplete { get; private set; }

        public void Disable()
        {
            if (!gameObject.activeInHierarchy) return;

            if (HintCache.Instance.Contains(this)) HintCache.Instance.Remove(this);

            DoDisappearAnimation(
                _appearDuration,
                () =>
                    {
                        IsEnabled = false;
                        _text.SetText(string.Empty);
                        gameObject.SetActive(false);
                    });
        }

        public void Enable()
        {
            if (IsEnabled) return;

            IsEnabled = true;

            _text.SetText(string.Empty);
            gameObject.SetActive(true);

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                _canvas.worldCamera = Camera.main;
            }

            if (!HintCache.Instance.Contains(this)) HintCache.Instance.Add(this);

            DoAppearAnimation(_appearDuration);
        }

        public void Initialize(Transform follow, string word)
        {
            _followTarget = follow;
            _text.SetText(word);
        }

        public void OnWordTyped(string letters)
        {
            WordWasInvalid = false;
            WordComplete = false;

            var word = _text.GetParsedText();
            var newText = word;

            if (string.Equals(letters, word))
            {
                DoCompleteWordAnimation(_wordCompleteAnimationDuration);
                WordComplete = true;
                newText = string.Format("<u>{0}</u>", word);
            }
            else if (word.StartsWith(letters))
            {
                var firstPart = word.Substring(0, letters.Length);
                newText = string.Format("<u>{0}</u>{1}", firstPart, word.Substring(letters.Length));
            }
            else
            {
                WordWasInvalid = true;
            }

            _text.SetText(newText);
        }

        private void DoAppearAnimation(float duration)
        {
            KillCurrentTweens();

            var scaleTween = transform.DOScale(Vector3.one, duration * 1.0f).SetEase(Ease.OutBack);
            _currentTweens.Add(scaleTween);

            if (_followTarget == null)
            {
                var moveTween = DOTween.To(
                    () => transform.localPosition,
                    x => transform.localPosition = x,
                    new Vector3(0, duration * 0.333f),
                    duration).SetRelative(true).OnComplete(() => _follow = true);

                _currentTweens.Add(moveTween);
            }
            else
            {
                var moveTween = transform.DOMove(new Vector3(0, duration * 0.333f), duration).SetRelative(true)
                    .OnComplete(() => _follow = true);

                _currentTweens.Add(moveTween);
            }
        }

        private void DoDisappearAnimation(float duration, Action onCompleted)
        {
            KillCurrentTweens();

            var scaleTween = transform.DOScale(Vector3.zero, duration * 1.0f).SetEase(Ease.OutBack)
                .OnComplete(() => onCompleted());
            _currentTweens.Add(scaleTween);

            if (_followTarget == null)
            {
                var moveTween = DOTween.To(
                    () => transform.localPosition,
                    x => transform.localPosition = x,
                    -new Vector3(0, duration * 0.333f),
                    duration).SetRelative();

                _currentTweens.Add(moveTween);
            }
            else
            {
                var moveTween = transform.DOMove(new Vector3(0, duration * 0.333f), duration).SetRelative(true);
                _currentTweens.Add(moveTween);
            }
        }

        private void DoCompleteWordAnimation(float duration)
        {
            var scaleSequence = DOTween.Sequence();
            scaleSequence.Append(_text.transform.DOScale(0.25f, duration).SetRelative().SetEase(Ease.OutBack));
            scaleSequence.Append(_text.transform.DOScale(-0.25f, duration).SetRelative().SetEase(Ease.OutBack));

            var colorSequence = DOTween.Sequence();
            colorSequence.Append(_text.DOColor(Color.yellow, duration).SetEase(Ease.OutBack));
            colorSequence.Append(_text.DOColor(Color.white, duration).SetEase(Ease.OutBack));
        }

        private void KillCurrentTweens()
        {
            foreach (var tween in _currentTweens) if (tween.IsPlaying()) DOTween.Kill(tween.id, true);

            _currentTweens.Clear();
        }

        private void LateUpdate()
        {
            if (!_follow) return;
            if (_followTarget == null) return;

            var newPos = Vector3.Lerp(
                transform.position,
                new Vector2(_followTarget.position.x, _followTarget.position.y) + _followOffset,
                _movementSpeed * Time.deltaTime);

            transform.position = newPos;
        }

        private void Start()
        {
            if (!HintCache.Instance.Contains(this)) HintCache.Instance.Add(this);
        }
    }
}