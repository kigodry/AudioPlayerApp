using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Threading;

namespace AudioPlayerApp
{
    public partial class MainWindow : Window
    {
        private List<string> musicFiles;
        private int currentTrackIndex;
        private bool isPlaying;
        private bool isRepeatMode;
        private bool isShuffleMode;
        private CancellationTokenSource sliderUpdateTokenSource;
        private CancellationTokenSource timerTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            musicFiles = new List<string>();
            currentTrackIndex = 0;
            isPlaying = false;
            isRepeatMode = false;
            isShuffleMode = false;
            sliderUpdateTokenSource = new CancellationTokenSource();
            timerTokenSource = new CancellationTokenSource();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Аудио файлы (*.mp3;*.wav;*.m4a)|*.mp3;*.wav;*.m4a"
            };

            if (dialog.ShowDialog() == true)
            {
                musicFiles = dialog.FileNames.ToList();

                if (musicFiles.Count > 0)
                {
                    currentTrackIndex = 0;
                    PlayMusic();
                }
            }
        }

        private void PlayMusic()
        {
            var trackFilePath = musicFiles[currentTrackIndex];
            mediaElement.Source = new Uri(trackFilePath);
            mediaElement.Play();

            isPlaying = true;
            playPauseButton.Content = "Пауза";

            StartSliderUpdateTask();
            StartTimerTask();
        }

        private void playPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaElement.Pause();
                isPlaying = false;
                playPauseButton.Content = "Воспроизвести";
            }
            else
            {
                mediaElement.Play();
                isPlaying = true;
                playPauseButton.Content = "Пауза";
            }
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex > 0)
            {
                currentTrackIndex--;
                PlayMusic();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex < musicFiles.Count - 1)
            {
                currentTrackIndex++;
                PlayMusic();
            }
        }

        private void repeatButton_Click(object sender, RoutedEventArgs e)
        {
            isRepeatMode = !isRepeatMode;
            repeatButton.Content = isRepeatMode ? "Повтор On" : "Повтор Off";
        }

        private void shuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffleMode = !isShuffleMode;
            shuffleButton.Content = isShuffleMode ? "Перемешать On" : "Перемешать Off";

            if (isShuffleMode)
            {
                var rng = new Random();
                musicFiles = musicFiles.OrderBy(x => rng.Next()).ToList();
            }
            else
            {
                musicFiles.Sort();
            }
        }

        private void StartSliderUpdateTask()
        {
            sliderUpdateTokenSource.Cancel();
            sliderUpdateTokenSource = new CancellationTokenSource();

            var token = sliderUpdateTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() => { UpdateSliderPosition(); });
                }
            }, token);
        }

        private void UpdateSliderPosition()
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                var totalTime = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                var currentTime = mediaElement.Position.TotalSeconds;
                musicSlider.Maximum = totalTime;
                musicSlider.Value = currentTime;

                var remainingTime = TimeSpan.FromSeconds(totalTime - currentTime);
                timerLabel.Text = $"{remainingTime:mm\\:ss}";
            }
        }

        private void StartTimerTask()
        {
            timerTokenSource.Cancel();
            timerTokenSource = new CancellationTokenSource();

            var token = timerTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() => { CheckPlaybackEnd(); });
                }
            }, token);
        }

        private void CheckPlaybackEnd()
        {
            if (mediaElement.HasAudio && mediaElement.Position >= mediaElement.NaturalDuration)
            {
                if (isRepeatMode)
                {
                    PlayMusic();
                }
                else
                {
                    if (currentTrackIndex < musicFiles.Count - 1)
                    {
                        currentTrackIndex++;
                        PlayMusic();
                    }
                }
            }
        }

        private void musicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                var newPosition = TimeSpan.FromSeconds(e.NewValue);
                mediaElement.Position = newPosition;
            }
        }
    }
}