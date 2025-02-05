using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json.Serialization;



public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _token;
    public bool Auth { get; private set; } = false;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }


    #region User
    public async Task<User?> GetCurrentUser()
    {
        return await GetAuthorizedAsync<User>("/api/user");
    }

    public async Task<string> GetRawResponse(string endpoint)
    {
        var response = await _httpClient.GetStringAsync(endpoint);
        return response;
    }

    public async Task<bool> Logout()
    {
        try
        {
            // Выполняем запрос на выход
            var response = await PostAuthorizedAsync<object, LogoutResponse>("/api/logout", null);

            // Если сообщение от сервера получено успешно
            if (response != null && response.Message == "Выход выполнен успешно.")
            {
                _token = null;
                Auth = false;
                return true;
            }

            // Если сообщение не соответствует ожидаемому
            Console.WriteLine("Не удалось выполнить выход пользователя. Ответ от сервера: " + (response?.Message ?? "Ответ пустой"));
            return false;
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            Console.WriteLine("Произошла ошибка при выходе пользователя:");
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }



    public async Task<bool> RegisterUser(User user)
    {
        try
        {
            Console.WriteLine("Начинаем запрос для регистрации пользователя...");

            // Формируем URL для запроса регистрации
            var requestUrl = "/api/register";
            Console.WriteLine($"Формируем URL: {requestUrl}");

            // Создаем MultipartFormDataContent для отправки формы с файлом и текстовыми данными
            var formData = new MultipartFormDataContent();
            Console.WriteLine("Создаем MultipartFormDataContent для отправки данных...");

            // Добавляем текстовые поля
            formData.Add(new StringContent(user.Username), "username");
            formData.Add(new StringContent(user.Email), "email");
            Console.WriteLine($"Добавлены текстовые поля: Username = {user.Username}, Email = {user.Email}");

            // Если есть пароль, добавляем его
            if (!string.IsNullOrEmpty(user.Password))
            {
                formData.Add(new StringContent(user.Password), "password");
                Console.WriteLine("Добавлен пароль");
            }

            // Добавляем поле для подтверждения пароля
            if (!string.IsNullOrEmpty(user.Password_confirmation))
            {
                formData.Add(new StringContent(user.Password_confirmation), "password_confirmation");
                Console.WriteLine("Добавлено подтверждение пароля");
            }

            // Если есть пол, добавляем его
            if (!string.IsNullOrEmpty(user.Gender))
            {
                formData.Add(new StringContent(user.Gender), "gender");
                Console.WriteLine($"Добавлен пол: {user.Gender}");
            }

            // Проверяем, есть ли изображение (например, аватар), и добавляем его
            if (!string.IsNullOrEmpty(user.Avatar))
            {
                var avatarFilePath = user.Avatar; // путь к изображению
                Console.WriteLine($"Добавляем аватар с пути: {avatarFilePath}");

                var fileContent = new ByteArrayContent(File.ReadAllBytes(avatarFilePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");  // или другой формат изображения
                formData.Add(fileContent, "avatar", Path.GetFileName(avatarFilePath));
                Console.WriteLine("Аватар добавлен в форму");
            }
            else
            {
                Console.WriteLine("Аватар не был передан.");
            }

            // Выполняем POST-запрос на регистрацию
            Console.WriteLine("Отправка POST-запроса на регистрацию...");
            var response = await _httpClient.PostAsync(requestUrl, formData);

            // Проверяем, был ли запрос успешным
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Регистрация прошла успешно.");
                return true;
            }
            else
            {
                // Логируем подробности ответа
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при регистрации. Статус: {response.StatusCode}, Ответ: {responseBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            Console.WriteLine($"Ошибка при регистрации пользователя: {ex.Message}");
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");

            // Дополнительные логи
            if (ex is FileNotFoundException)
            {
                Console.WriteLine("Ошибка: Не найден файл аватара.");
            }

            return false;
        }
    }



    #endregion

    #region Authentication
    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        Auth = true;
    }

    public async Task Login(AuthRequest request)
    {
        var token = await PostAsync<AuthRequest, AuthResponse>("/login", request);
        if (token?.Token != null)
        {
            SetToken(token.Token);
            Console.WriteLine("User login successful.");
        }
        else
        {
            Console.WriteLine("User login failed.");
        }
    }

    public async Task Login(ApiRequestAdmin request)
    {
        var token = await PostAsync<ApiRequestAdmin, AuthResponse>("/api/login", request);
        if (token?.Token != null)
        {
            SetToken(token.Token);
            Console.WriteLine("Admin login successful.");
        }
        else
        {
            Console.WriteLine("Admin login failed.");
        }
    }

    public async Task<bool> Register(RegisterRequest request)
    {
        var response = await PostAsync<RegisterRequest, object>("/register", request);
        if (response != null)
        {
            Console.WriteLine("Registration successful, logging in...");
            await Login(new AuthRequest { Username = request.Username, Password = request.Password });
            return true;
        }
        Console.WriteLine("Registration failed.");
        return false;
    }



    #endregion

    #region Movies
    public async Task<List<Movie>?> GetMovies()
    {
        return await GetAuthorizedAsync<List<Movie>>("/api/movies");
    }

    public async Task<Movie?> GetMovieById(int movieId)
    {
        return await GetAuthorizedAsync<Movie>($"/api/movies/{movieId}");
    }

    public async Task<Movie?> UpdateMovie(int movieId, Movie movie, string filePath)
    {
        if (!Auth)
        {
            Console.WriteLine("[Ошибка] Пользователь не аутентифицирован.");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        using var formData = new MultipartFormDataContent();
        FileStream fileStream = null;

        try
        {
            Console.WriteLine("[Отладка] Добавляем текстовые данные фильма...");
            formData.Add(new StringContent(movie.Title), "title");
            formData.Add(new StringContent(movie.ReleaseYear.ToString()), "release_year");
            formData.Add(new StringContent(movie.Duration.ToString()), "duration");
            formData.Add(new StringContent(movie.Description), "description");
            formData.Add(new StringContent(movie.Studio.Id.ToString()), "studio_id");
            formData.Add(new StringContent(movie.age_rating.Id.ToString()), "age_rating_id");
            Console.WriteLine("[Отладка] Текстовые данные успешно добавлены.");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                Console.WriteLine($"[Отладка] Загружаем файл: {filePath}");
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var streamContent = new StreamContent(fileStream);
                formData.Add(streamContent, "photo", Path.GetFileName(filePath));
                Console.WriteLine("[Отладка] Файл добавлен в форму данных.");
            }
            else
            {
                Console.WriteLine("[Предупреждение] Файл не найден или путь пуст. Фильм будет обновлён без изменения изображения.");
            }

            Console.WriteLine("[Отладка] Отправляем HTTP-запрос...");
            var response = await _httpClient.PostAsync($"/api/movies/update/{movieId}", formData);

            if (fileStream != null)
            {
                Console.WriteLine("[Отладка] Закрываем поток файла...");
                fileStream.Close();
            }

            Console.WriteLine($"[Отладка] Ответ от сервера: {(int)response.StatusCode} {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[Отладка] Тело ответа:");
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка] Не удалось обновить фильм.");
                throw new Exception($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
            }

            Console.WriteLine("[Отладка] Десериализуем ответ...");
            var result = JsonSerializer.Deserialize<Movie>(responseContent);
            Console.WriteLine($"[Отладка] Фильм успешно обновлён: {result?.Title}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Ошибка] Произошла ошибка при обновлении фильма:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw;
        }
        finally
        {
            fileStream?.Dispose();
        }
    }


    public async Task<bool> DeleteMovie(int movieId)
    {
        return await DeleteAuthorizedAsync($"/api/movies/{movieId}");
    }

    public async Task<Movie?> AddMovie(Movie movie, string filePath)
    {
        if (!Auth)
        {
            Console.WriteLine("[Ошибка] Пользователь не аутентифицирован.");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        using var formData = new MultipartFormDataContent();
        FileStream fileStream = null;

        try
        {
            Console.WriteLine("[Отладка] Добавляем текстовые данные фильма...");
            formData.Add(new StringContent(movie.Title), "title");
            formData.Add(new StringContent(movie.ReleaseYear.ToString()), "release_year");
            formData.Add(new StringContent(movie.Duration.ToString()), "duration");
            formData.Add(new StringContent(movie.Description), "description");
            formData.Add(new StringContent(movie.Studio.Id.ToString()), "studio_id");
            formData.Add(new StringContent(movie.age_rating.Id.ToString()), "age_rating_id");
            Console.WriteLine("[Отладка] Текстовые данные успешно добавлены.");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                Console.WriteLine($"[Отладка] Загружаем файл: {filePath}");
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var streamContent = new StreamContent(fileStream);
                formData.Add(streamContent, "photo", Path.GetFileName(filePath));
                Console.WriteLine("[Отладка] Файл добавлен в форму данных.");
            }
            else
            {
                Console.WriteLine("[Предупреждение] Файл не найден или путь пуст. Фильм будет добавлен без изображения.");
            }

            Console.WriteLine("[Отладка] Отправляем HTTP-запрос...");
            var response = await _httpClient.PostAsync("/api/movies/create", formData);

            // Закрытие потока файла теперь гарантировано
            if (fileStream != null)
            {
                Console.WriteLine("[Отладка] Закрываем поток файла...");
                fileStream.Close();
            }

            Console.WriteLine($"[Отладка] Ответ от сервера: {(int)response.StatusCode} {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[Отладка] Тело ответа:");
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка] Не удалось добавить фильм.");
                throw new Exception($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
            }

            Console.WriteLine("[Отладка] Десериализуем ответ...");
            var result = JsonSerializer.Deserialize<Movie>(responseContent);
            Console.WriteLine($"[Отладка] Фильм успешно добавлен: {result?.Title}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Ошибка] Произошла ошибка при добавлении фильма:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw;
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

    #endregion

    #region Actors
    public async Task<List<Actor>?> GetActors()
    {
        return await GetAuthorizedAsync<List<Actor>>("/api/actors");
    }

    public async Task<Actor?> GetActorById(int actorId)
    {
        return await GetAuthorizedAsync<Actor>($"/api/actors/{actorId}");
    }

    public async Task<Actor?> AddActor(Actor actor, string filePath)
    {
        if (!Auth)
        {
            Console.WriteLine("[Ошибка] Пользователь не аутентифицирован.");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        using var formData = new MultipartFormDataContent();
        FileStream fileStream = null;

        try
        {
            Console.WriteLine("[Отладка] Добавляем текстовые данные актёра...");
            formData.Add(new StringContent(actor.FirstName), "first_name");
            formData.Add(new StringContent(actor.LastName), "last_name");
            formData.Add(new StringContent(actor.BirthDate.ToString("yyyy-MM-dd")), "birth_date");
            formData.Add(new StringContent(actor.Biography ?? string.Empty), "biography");
            Console.WriteLine("[Отладка] Текстовые данные успешно добавлены.");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                Console.WriteLine($"[Отладка] Загружаем файл: {filePath}");
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var streamContent = new StreamContent(fileStream);
                formData.Add(streamContent, "photo", Path.GetFileName(filePath));
                Console.WriteLine("[Отладка] Файл добавлен в форму данных.");
            }
            else
            {
                Console.WriteLine("[Предупреждение] Файл не найден или путь пуст. Актёр будет добавлен без изображения.");
            }

            Console.WriteLine("[Отладка] Отправляем HTTP-запрос...");
            var response = await _httpClient.PostAsync("/api/actors", formData);

            // Закрытие потока файла теперь гарантировано
            if (fileStream != null)
            {
                Console.WriteLine("[Отладка] Закрываем поток файла...");
                fileStream.Close();
            }

            Console.WriteLine($"[Отладка] Ответ от сервера: {(int)response.StatusCode} {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[Отладка] Тело ответа:");
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка] Не удалось добавить актёра.");
                throw new Exception($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
            }

            Console.WriteLine("[Отладка] Десериализуем ответ...");
            var result = JsonSerializer.Deserialize<Actor>(responseContent);
            Console.WriteLine($"[Отладка] Актёр успешно добавлен: {result?.FirstName} {result?.LastName}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Ошибка] Произошла ошибка при добавлении актёра:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw;
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

    public async Task<Actor?> UpdateActor(Actor actor, string? filePath)
    {
        if (!Auth)
        {
            Console.WriteLine("[Ошибка] Пользователь не аутентифицирован.");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        using var formData = new MultipartFormDataContent();

        try
        {
            Console.WriteLine("[Отладка] Добавляем текстовые данные актера...");
            formData.Add(new StringContent(actor.FirstName), "name");
            formData.Add(new StringContent(actor.BirthDate.ToString("yyyy-MM-dd")), "birth_date");
            formData.Add(new StringContent(actor.Biography), "biography");
            Console.WriteLine("[Отладка] Текстовые данные успешно добавлены.");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var streamContent = await GetPhotoContentAsync(filePath);
                    formData.Add(streamContent, "photo", Path.GetFileName(filePath));
                    Console.WriteLine("[Отладка] Файл добавлен в форму данных.");
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"[Предупреждение] {ex.Message}");
                }
            }
            else
            {
                formData.Add(new StringContent(actor.PhotoFilePath ?? string.Empty), "photo");
                Console.WriteLine("[Отладка] Используется предыдущее фото актера.");
            }

            Console.WriteLine("[Отладка] Отправляем HTTP-запрос...");
            var response = await _httpClient.PostAsync($"/api/actors/update/{actor.Id}", formData);

            Console.WriteLine($"[Отладка] Ответ от сервера: {(int)response.StatusCode} {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[Отладка] Тело ответа:");
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[Ошибка] Не удалось обновить актера.");
                throw new Exception($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
            }

            Console.WriteLine("[Отладка] Десериализуем ответ...");
            var result = JsonSerializer.Deserialize<Actor>(responseContent);
            Console.WriteLine($"[Отладка] Актер успешно обновлен: {result?.FirstName}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Ошибка] Произошла ошибка при обновлении актера:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }


    #endregion

    #region Reviews

    public async Task<List<MovieRating>?> GetReviews()
    {
        return await GetAuthorizedAsync<List<MovieRating>>("/api/ratings");
    }

    public async Task<MovieRating?> GetReviewById(int reviewId)
    {
        return await GetAuthorizedAsync<MovieRating>($"/api/ratings/{reviewId}");
    }

    public async Task<bool> DeleteReview(int reviewId)
    {
        return await DeleteAuthorizedAsync($"/api/ratings/{reviewId}");
    }

    public async Task<MovieRating?> AddMovieRating(MovieRating movieRating)
    {
        return await PostAuthorizedAsync<MovieRating, MovieRating>("/api/ratings", movieRating);
    }

    public async Task<MovieRating?> UpdateMovieRating(int ratingId, MovieRating movieRating)
    {
        return await PostAuthorizedAsync<MovieRating, MovieRating>($"/api/ratings/{ratingId}", movieRating);
    }





    #endregion

    #region Studios

    public async Task<List<Studio>?> GetStudios()
    {
        return await GetAuthorizedAsync<List<Studio>>("/api/studios");
    }

    public async Task<Studio?> GetStudioById(int studioId)
    {
        return await GetAuthorizedAsync<Studio>($"/api/studios/{studioId}");
    }

    public async Task<bool> DeleteStudio(int studioId)
    {
        return await DeleteAuthorizedAsync($"/api/studios/{studioId}");
    }

    public async Task<Studio?> AddStudio(Studio studio)
    {
        return await PostAuthorizedAsync<Studio, Studio>("/api/studios", studio);
    }

    public async Task<Studio?> UpdateStudio(int studioId, Studio studio)
    {
        // Формируем URL для PUT-запроса с ID студии
        var url = $"/api/studios/{studioId}";

        // Отправляем PUT-запрос с обновлённой студией
        return await PostAuthorizedAsync<Studio, Studio>(url, studio);
    }


    #endregion

    #region Genre
    public async Task<List<Genre>?> GetGenres()
    {
        return await GetAuthorizedAsync<List<Genre>>("/api/genres");
    }

    public async Task<Genre?> GetGenreById(int genreId)
    {
        return await GetAuthorizedAsync<Genre>($"/api/genres/{genreId}");
    }

    public async Task<bool> DeleteGenre(int genreId)
    {
        return await DeleteAuthorizedAsync($"/api/genres/{genreId}");
    }

    public async Task<Genre?> AddGenre(Genre genre)
    {
        return await PostAuthorizedAsync<Genre, Genre>("/api/genres", genre);
    }

    public async Task<Genre?> UpdateGenre(int genreId, Genre genre)
    {
        try
        {
            // Путь для обновления жанра через POST
            var url = $"/api/genres/{genreId}";

            // Отправляем запрос на обновление жанра
            return await PostAuthorizedAsync<Genre, Genre>(url, genre); // Используем POST
        }
        catch (Exception ex)
        {
            Console.WriteLine("Произошла ошибка при обновлении жанра:");
            Console.WriteLine($"Ошибка: {ex.Message}");
            return null;
        }
    }



    #endregion

    #region Helpers

    private async Task<StreamContent> GetPhotoContentAsync(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out Uri? uri) && uri.Scheme.StartsWith("http"))
        {
            Console.WriteLine($"[Отладка] Загружаем изображение с URL: {filePath}");
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(filePath);

            if (!response.IsSuccessStatusCode)
            {
                throw new FileNotFoundException($"Не удалось загрузить изображение: {response.StatusCode}");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return new StreamContent(stream);
        }
        else if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            Console.WriteLine($"[Отладка] Загружаем локальный файл: {filePath}");
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new StreamContent(fileStream);
        }

        throw new FileNotFoundException("Файл не найден или путь пуст.");
    }

    private async Task<T?> GetAuthorizedAsync<T>(string endpoint)
    {
        if (!Auth) throw new UnauthorizedAccessException("User is not authenticated.");
        var response = await _httpClient.GetAsync(endpoint);
        return await HandleResponse<T>(response);
    }

    private async Task<TResponse?> PostAuthorizedAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        if (!Auth)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,  
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Пропускает null, если API не требует их
        };

        var jsonContent = JsonSerializer.Serialize(data, options);

        // Преобразуем объект данных в JSON
        //var jsonContent = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Выполняем POST-запрос
        var response = await _httpClient.PostAsync(endpoint, content);

        // Логирование ошибки с подробностями
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ от сервера: {responseContent}");
            throw new Exception($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
        }

        // Преобразуем ответ в объект типа TResponse
        var responseContentSuccess = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TResponse>(responseContentSuccess);

        return result;
    }

    private async Task<bool> DeleteAuthorizedAsync(string endpoint)
    {
        if (!Auth) throw new UnauthorizedAccessException("User is not authenticated.");
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        return await HandleResponse<TResponse>(response);
    }

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error ({response.StatusCode}): {error}");
            return default;
        }
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    #endregion

    #region Favorite
    public async Task<List<Favorite>?> GetFavoriteMovies(int userId)
    {
        return await GetAuthorizedAsync<List<Favorite>>($"/api/movies/favorites{userId}");
    }

    private async Task<T?> GetAuthorizedAsync<T>(string endpoint, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Добавляем токен в заголовки запроса
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Выполняем GET-запрос
        var response = await _httpClient.GetAsync(endpoint);

        // Обрабатываем ответ
        return await HandleResponse<T>(response);
    }

    public async Task<List<Favorite>?> GetFavoriteMovies(string token)
    {
        // Формируем URL для получения списка любимых фильмов
        string endpoint = "/api/movies/favorites";

        // Выполняем GET-запрос с авторизацией по токену
        return await GetAuthorizedAsync<List<Favorite>>(endpoint, token);
    }

    public async Task<bool> RemoveMovieFromFavorites(int movieId, string token)
    {

        string endpoint = $"/api/movies/{movieId}/favorite";

        try
        {
            // Выполняем DELETE-запрос с авторизацией по токену
            var response = await _httpClient.DeleteAsync(endpoint);

            // Проверка на успешный ответ от сервера (статус код 200-299)
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Фильм успешно удалён из избранного.");
                return true;
            }
            else
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при удалении фильма: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Произошла ошибка при удалении фильма:");
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AddMovieToFavorites(int movieId, string token)
    {
        string endpoint = $"/api/movies/{movieId}/favorite";

        try
        {
            // Добавляем токен в заголовок Authorization
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Выполняем POST-запрос
            var response = await _httpClient.PostAsync(endpoint, null);

            // Проверка на успешный ответ от сервера (статус код 200-299)
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Фильм успешно добавлен в избранное.");
                return true;
            }
            else
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при добавлении фильма в избранное: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Произошла ошибка при добавлении фильма в избранное:");
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }
    #endregion
}

class Program
{
static async Task Main(string[] args)
{
    string baseUrl = "https://1a7f0347-4740-48dd-a191-0d726e603854.tunnel4.com";
    const string testToken = "5|sGTKmlKxXrZkBCrsF4PIBgroGr2tYYzZwndAqwMbdd9adcbd";

    var apiClient = new ApiClient(baseUrl);

     apiClient.SetToken(testToken);

        #region API ADMIN
        //Console.WriteLine("Logging in as admin...");

        //var adminRequest = new ApiRequestAdmin
        //{
        //    Email = "admin@example.com",
        //    Password = "password"
        //};

        //Console.WriteLine($"Отправка запроса с email: {adminRequest.Email}, password: {adminRequest.Password}");
        //await apiClient.Login(adminRequest);


        #endregion


        #region Test GetAllMovies 
        //try
        //{
        //    Console.WriteLine("Начинаем запрос списка фильмов...");

        //    // Выполняем запрос для получения списка фильмов
        //    var movies = await apiClient.GetMovies();

        //    if (movies != null)
        //    {
        //        Console.WriteLine("Список фильмов получен успешно.");
        //        Console.WriteLine($"Количество фильмов: {movies.Count}");

        //        foreach (var movie in movies)
        //        {
        //            Console.WriteLine($"ID: {movie.Id}, Название: {movie.Title}, Год: {movie.ReleaseYear}");
        //            Console.WriteLine($"Описание: {movie.Description}");
        //            Console.WriteLine($"Длительность: {movie.Duration} минут");
        //            Console.WriteLine($"Фото: {movie.Photo}");

        //            // Вывод информации о студии
        //            if (movie.Studio != null)
        //            {
        //                Console.WriteLine($"Студия: {movie.Studio.Name}");
        //            }
        //            else
        //            {
        //                Console.WriteLine("Студия: информация отсутствует");
        //            }

        //            // Вывод информации о возрастном рейтинге
        //            if (movie.age_rating != null)
        //            {
        //                Console.WriteLine($"Возрастной рейтинг: {movie.age_rating.Age}+");
        //            }
        //            else
        //            {
        //                Console.WriteLine("Возрастной рейтинг: информация отсутствует");
        //            }





        //            // Вывод информации о рейтингах
        //            if (movie.Rating != null && movie.Rating.Any())
        //            {
        //                Console.WriteLine("Отзывы:");
        //                foreach (var rating in movie.Rating)
        //                {
        //                    Console.WriteLine($"- Пользователь ID {rating.UsersId}: {rating.ReviewText} (Дата: {rating.CreatedAt})");
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine("Отзывы: информация отсутствует");
        //            }

        //            Console.WriteLine(new string('-', 40)); // Разделитель между фильмами
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось получить список фильмов.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем полную ошибку
        //    Console.WriteLine("Произошла ошибка при получении списка фильмов:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Проверка на тип ошибки, если это конкретная ошибка десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }

        //    // Также можно вывести stack trace ошибки, если это необходимо
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test GetMovieById
        //try
        //{
        //    int testMovieId = 1; // Укажите ID фильма для теста
        //    Console.WriteLine($"Начинаем запрос фильма с ID: {testMovieId}...");

        //    // Выполняем запрос на получение фильма по ID
        //    var movie = await apiClient.GetMovieById(testMovieId);

        //    if (movie != null)
        //    {
        //        Console.WriteLine("Фильм получен успешно.");
        //        Console.WriteLine($"ID: {movie.Id}, Название: {movie.Title}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Фильм с ID {testMovieId} не найден.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при получении фильма:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Проверка на ошибку JSON-десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }

        //    // Выводим stack trace ошибки
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test UpdateMovie
        //try
        //{
        //    int testMovieId = 12;
        //    Console.WriteLine($"Начинаем обновление фильма с ID: {testMovieId}...");

        //    // Создайте объект фильма для обновления
        //    var updatedMovie = new Movie
        //    {
        //        Id = testMovieId,
        //        Title = "llfasdfljahsdflkhja slkdfj alksdjf", // Пример обновления названия
        //        ReleaseYear = 2025,            // Пример обновления года
        //        Duration = 120,                // Пример обновления продолжительности
        //        Description = "Updated description for the movie.", // Пример обновления описания
        //        Studio = new Studio { Id = 1, Name = "Warner Bros" }, // Пример обновления студии
        //        age_rating = new AgeRating { Id = 3, Age = 16 }, // Пример обновления рейтинга
        //        Rating = new List<MovieRating>(), // Если нужно, добавьте рейтинг
        //    };

        //    string filePath = "istockphoto-1494804150-612x612.jpg"; // Путь к обновляемому изображению

        //    // Выводим объект для отладки перед отправкой
        //    Console.WriteLine("Объект для обновления:");
        //    Console.WriteLine($"Название: {updatedMovie.Title}, Год: {updatedMovie.ReleaseYear}");

        //    // Выполняем запрос на обновление фильма
        //    var updatedMovieResponse = await apiClient.UpdateMovie(testMovieId, updatedMovie, filePath);

        //    // Проверяем ответ от API
        //    if (updatedMovieResponse != null)
        //    {
        //        Console.WriteLine("Фильм успешно обновлен.");
        //        Console.WriteLine($"ID: {updatedMovieResponse.Id}, Название: {updatedMovieResponse.Title}, Год: {updatedMovieResponse.ReleaseYear}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось обновить фильм с ID {testMovieId}. Ответ от API пустой.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при обновлении фильма:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Проверка на ошибку JSON-десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }

        //    // Выводим stack trace ошибки
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion


        #region Test DeleteMovie
        //try
        //{
        //    Console.WriteLine("Начинаем запрос на удаление фильма...");

        //    // Указываем ID фильма, который хотим удалить
        //    int movieId = 5; // Пример ID фильма
        //    Console.WriteLine($"Попытка удалить фильм с ID: {movieId}");

        //    // Сначала проверяем, существует ли фильм с таким ID
        //    var movie = await apiClient.GetMovieById(movieId);
        //    if (movie == null)
        //    {
        //        Console.WriteLine($"Фильм с ID {movieId} не найден в базе.");
        //        return;
        //    }

        //    // Выполняем запрос на удаление
        //    var result = await apiClient.DeleteMovie(movieId);

        //    // Проверяем результат удаления
        //    if (result)
        //    {
        //        Console.WriteLine($"Фильм с ID {movieId} был успешно удален.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось удалить фильм с ID {movieId}. Возможно, фильм не существует или произошла ошибка на сервере.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Произошла ошибка при удалении фильма: {ex.Message}");
        //}
        #endregion

        #region Test AddMovie
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления фильма...");

        //    var newMovie = new Movie
        //    {
        //        Title = "New Filmmm",
        //        ReleaseYear = 2014,
        //        Duration = 169,
        //        Description = "A sci-fi masterpiece by Christopher Nolan.",
        //        Studio = new Studio { Id = 2 }, // ID студии
        //        AgeRating = new AgeRating { Id = 3 } // Возрастной рейтинг
        //    };

        //    string posterPath = "D:\\pgoto.jpg";

        //    // Выполняем запрос на добавление фильма
        //    var addedMovie = await apiClient.AddMovie(newMovie, posterPath);

        //    // Проверяем, что фильм был добавлен
        //    if (addedMovie != null)
        //    {
        //        Console.WriteLine($"Фильм успешно добавлен: {addedMovie.Title}");
        //        Console.WriteLine($"ID фильма: {addedMovie.Id}");
        //        Console.WriteLine($"Год выпуска: {addedMovie.ReleaseYear}");
        //        Console.WriteLine($"Продолжительность: {addedMovie.Duration} минут");
        //        Console.WriteLine($"Описание: {addedMovie.Description}");
        //        Console.WriteLine($"Студия: {addedMovie.Studio?.Id}");
        //        Console.WriteLine($"Возрастной рейтинг: {addedMovie.AgeRating?.Id}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить фильм.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при добавлении фильма:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion



        #region Test GetActors
        //try
        //{
        //    Console.WriteLine("Начинаем запрос списка актеров...");

        //    // Выполняем запрос для получения списка актеров
        //    var actors = await apiClient.GetActors();

        //    if (actors != null)
        //    {
        //        Console.WriteLine("Список актеров получен успешно.");
        //        Console.WriteLine($"Количество актеров: {actors.Count}");

        //        foreach (var actor in actors)
        //        {
        //            Console.WriteLine($"ID: {actor.Id}, Имя: {actor.FirstName} {actor.LastName}, Дата рождения: {actor.BirthDate:yyyy-MM-dd}, Биография: {actor.Biography}, Фото: {actor.PhotoFilePath}");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось получить список актеров.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем полную ошибку
        //    Console.WriteLine("Произошла ошибка при получении списка актеров:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Проверка на тип ошибки, если это конкретная ошибка десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }

        //    // Также можно вывести stack trace ошибки, если это необходимо
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test GetActorById
        //try
        //{
        //    Console.WriteLine("Начинаем запрос актера по ID...");

        //    // Указываем ID актера, которого хотим получить
        //    int actorId = 1; // Пример ID актера

        //    // Выполняем запрос для получения актера по ID
        //    var actor = await apiClient.GetActorById(actorId);

        //    if (actor != null)
        //    {
        //        Console.WriteLine("Актер получен успешно.");
        //        Console.WriteLine($"ID: {actor.Id}, Имя: {actor.FirstName} {actor.LastName}, Дата рождения: {actor.BirthDate:yyyy-MM-dd}, Биография: {actor.Biography}, Фото: {actor.Photo}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Актер с ID {actorId} не найден.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем полную ошибку
        //    Console.WriteLine("Произошла ошибка при получении актера по ID:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Проверка на тип ошибки, если это конкретная ошибка десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }

        //    // Также можно вывести stack trace ошибки, если это необходимо
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test AddActor
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления актёра...");

        //    var newActor = new Actor
        //    {
        //        FirstName = "Leonardo new",
        //        LastName = "DiCaprio new",
        //        BirthDate = new DateTime(1974, 11, 11),
        //        Biography = "An acclaimed actor known for his roles in Titanic and Inception."
        //    };

        //    string photoPath = "D:\\pgoto.jpg"; // Укажите путь к фотографии актёра

        //    // Выполняем запрос на добавление актёра
        //    var addedActor = await apiClient.AddActor(newActor, photoPath);

        //    // Проверяем, что актёр был добавлен
        //    if (addedActor != null)
        //    {
        //        Console.WriteLine($"Актёр успешно добавлен: {addedActor.FirstName} {addedActor.LastName}");
        //        Console.WriteLine($"ID актёра: {addedActor.Id}");
        //        Console.WriteLine($"Дата рождения: {addedActor.BirthDate:yyyy-MM-dd}");
        //        Console.WriteLine($"Биография: {addedActor.Biography}");
        //        Console.WriteLine($"Путь к фото: {addedActor.PhotoFilePath}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить актёра.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при добавлении актёра:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test DeleteActor
        //try
        //{
        //    Console.WriteLine("Начинаем запрос на удаление актера...");

        //    // Указываем ID актера, которого хотим удалить
        //    int actorId = 2; // Пример ID актера

        //    Console.WriteLine($"Попытка удалить актера с ID: {actorId}");

        //    // Выполняем запрос для удаления актера
        //    var result = await apiClient.DeleteActor(actorId);

        //    // Проверяем результат удаления
        //    if (result)
        //    {
        //        Console.WriteLine($"Актер с ID {actorId} был успешно удален.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось удалить актера с ID {actorId}. Возможно, актера не существует.");
        //    }
        //}
        //catch (HttpRequestException httpEx)
        //{
        //    // Логируем ошибку при запросе
        //    Console.WriteLine("Произошла ошибка при отправке запроса на удаление актера:");
        //    Console.WriteLine($"Ошибка: {httpEx.Message}");
        //    Console.WriteLine($"Стек вызовов: {httpEx.StackTrace}");

        //    // Логирование дополнительно, например, статус код ответа, если доступен
        //    if (httpEx.InnerException is WebException webEx)
        //    {
        //        Console.WriteLine($"Статус ответа: {webEx.Status}");
        //        if (webEx.Response is HttpWebResponse response)
        //        {
        //            Console.WriteLine($"Ответ от сервера: {response.StatusCode} {response.StatusDescription}");
        //        }
        //    }
        //}
        //catch (JsonException jsonEx)
        //{
        //    // Логируем ошибку при обработке JSON
        //    Console.WriteLine("Ошибка при обработке ответа JSON:");
        //    Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //    Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //}
        //catch (Exception ex)
        //{
        //    // Логируем общие ошибки
        //    Console.WriteLine("Произошла непредвиденная ошибка:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test UpdateActor
        try
        {
            int testActorId = 5;
            Console.WriteLine($"Начинаем обновление актера с ID: {testActorId}...");

            // Создайте объект актера для обновления
            var updatedActor = new Actor
            {
                Id = testActorId,
                FirstName = "Updated Name",  // Пример обновления имени
                BirthDate = new DateTime(1980, 5, 15),  // Пример обновления даты рождения
                Biography = "Updated biography for the actor.",  // Пример обновления биографии
                PhotoFilePath = "previous_photo.jpg" // Оставляем предыдущее фото, если новый путь не указан
            };

            string? filePath = "C:\\Users\\Alex\\Desktop\\updated_actor_photo.jpg"; // Пример нового фото

            // Выводим объект для отладки перед отправкой
            Console.WriteLine("Объект для обновления:");
            Console.WriteLine($"Имя: {updatedActor.FirstName}, Дата рождения: {updatedActor.BirthDate.ToShortDateString()}");

            // Выполняем запрос на обновление актера
            var updatedActorResponse = await UpdateActor(testActorId, updatedActor, filePath);

            // Проверяем ответ от API
            if (updatedActorResponse != null)
            {
                Console.WriteLine("Актер успешно обновлен.");
                Console.WriteLine($"ID: {updatedActorResponse.Id}, Имя: {updatedActorResponse.Name}, Дата рождения: {updatedActorResponse.BirthDate.ToShortDateString()}");
            }
            else
            {
                Console.WriteLine($"Не удалось обновить актера с ID {testActorId}. Ответ от API пустой.");
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            Console.WriteLine("Произошла ошибка при обновлении актера:");
            Console.WriteLine($"Ошибка: {ex.Message}");

            // Проверка на ошибку JSON-десериализации
            if (ex is JsonException jsonEx)
            {
                Console.WriteLine("Ошибка в обработке JSON:");
                Console.WriteLine($"Ошибка: {jsonEx.Message}");
                Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
            }

            // Выводим stack trace ошибки
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        }
        #endregion





        #region Test GetCurrentUser
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для получения текущего пользователя...");

        //    // Выполняем запрос на получение текущего пользователя
        //    var user = await apiClient.GetCurrentUser();

        //    // Выводим сырые данные, которые вернул API
        //    Console.WriteLine("Сырой JSON ответ:");
        //    Console.WriteLine(await apiClient.GetRawResponse("/api/user"));

        //    // Проверяем, что пользователь не null
        //    if (user != null)
        //    {
        //        Console.WriteLine("Текущий пользователь получен успешно.");

        //        // Выводим информацию о пользователе
        //        Console.WriteLine($"ID: {user.Id}, Имя: {user.Username}, Почта: {user.Email}, Дата создания: {user.UpdatedAt:yyyy-MM-dd}");
        //    }
        //    else
        //    {
        //        // Если ответ пустой, выводим сообщение
        //        Console.WriteLine("Не удалось получить информацию о текущем пользователе.");
        //    }

        //    // Проверяем, есть ли у пользователя ожидаемые данные
        //    if (user != null && user.Id == 0)
        //    {
        //        Console.WriteLine("Предупреждение: Получен пользователь с ID == 0. Возможно, проблема с авторизацией.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при получении информации о текущем пользователе:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");

        //    // Проверка на тип ошибки, если это конкретная ошибка десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }
        //}
        #endregion

        #region Test Logout Не работает
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для выхода пользователя...");

        //    // Выполняем запрос на выход пользователя
        //    var logoutResult = await apiClient.Logout();

        //    // Проверяем, что выход успешен
        //    if (logoutResult)
        //    {
        //        Console.WriteLine("Пользователь успешно вышел.");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось выполнить выход пользователя.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при выходе пользователя:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");

        //    // Проверка на тип ошибки, если это конкретная ошибка десериализации
        //    if (ex is JsonException jsonEx)
        //    {
        //        Console.WriteLine("Ошибка в обработке JSON:");
        //        Console.WriteLine($"Ошибка: {jsonEx.Message}");
        //        Console.WriteLine($"Стек вызовов: {jsonEx.StackTrace}");
        //    }
        //}
        #endregion

        #region Test RegisterUser
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для регистрации пользователя...");

        //    var newUser = new User
        //    {
        //        Username = "newuser",
        //        Email = "newuser@example.com",
        //        Password = "securepassword123",  // Убедитесь, что пароль и подтверждение совпадают
        //        Password_confirmation = "securepassword123",  // Подтверждение пароля
        //        Gender = "male",  // Если нужно, добавьте пол
        //        Avatar = "D:\\pgoto.jpg"  // Путь к изображению аватара
        //    };

        //    // Выполняем запрос на регистрацию
        //    var registrationSuccess = await apiClient.RegisterUser(newUser);

        //    // Проверяем результат регистрации
        //    if (registrationSuccess)
        //    {
        //        Console.WriteLine("Пользователь успешно зарегистрирован!");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось зарегистрировать пользователя.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при регистрации пользователя:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion




        #region Test GetReviews
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для получения всех отзывов...");

        //    var reviews = await apiClient.GetReviews();

        //    if (reviews != null && reviews.Count > 0)
        //    {
        //        Console.WriteLine($"Успешно получено {reviews.Count} отзывов.");
        //        foreach (var review in reviews)
        //        {
        //            Console.WriteLine($"ID: {review.Id}, Фильм: {review.Movie.Title}, Оценка: {review.Rating}, Текст: {review.ReviewText}");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Отзывы не найдены.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка при получении отзывов:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test GetReviewById
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для получения отзыва по ID...");

        //    int reviewId = 1; // Пример ID отзыва
        //    var review = await apiClient.GetReviewById(reviewId);

        //    if (review != null)
        //    {
        //        Console.WriteLine("Отзыв получен успешно.");
        //        Console.WriteLine($"ID: {review.Id}, Фильм: {review.Movie.Title}, Оценка: {review.Rating}, Текст: {review.ReviewText}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Отзыв с ID {reviewId} не найден.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка при получении отзыва по ID:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test DeleteReview
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для удаления отзыва...");

        //    int reviewId = 1; // Пример ID отзыва
        //    Console.WriteLine($"Попытка удалить отзыв с ID: {reviewId}");

        //    bool isDeleted = await apiClient.DeleteReview(reviewId);

        //    if (isDeleted)
        //    {
        //        Console.WriteLine($"Отзыв с ID {reviewId} успешно удален.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось удалить отзыв с ID {reviewId}. Возможно, он уже был удален или не существует.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка при удалении отзыва:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test AddMovieRating
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления нового отзыва...");

        //    // Создаём новый объект отзыва
        //    var newMovieRating = new MovieRating
        //    {
        //        MoviesId = 1,  // ID фильма
        //        ReviewText = "Excellent movie with a great plot!"  // Текст отзыва
        //    };

        //    // Выполняем запрос на добавление отзыва
        //    var addedMovieRating = await apiClient.AddMovieRating(newMovieRating);

        //    // Проверяем, что отзыв был добавлен
        //    if (addedMovieRating != null)
        //    {
        //        Console.WriteLine("Отзыв успешно добавлен.");
        //        Console.WriteLine($"ID отзыва: {addedMovieRating.Id}");

        //        // Выводим текст отзыва, если он есть
        //        if (!string.IsNullOrEmpty(addedMovieRating.ReviewText))
        //        {
        //            Console.WriteLine($"Текст отзыва: {addedMovieRating.ReviewText}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Текст отзыва не указан.");
        //        }

        //        // Выводим даты создания и обновления, если они есть


        //        // Проверяем, есть ли данные о фильме и выводим их
        //        if (addedMovieRating.Movie != null)
        //        {
        //            Console.WriteLine($"Фильм, к которому относится отзыв:");
        //            Console.WriteLine($"ID фильма: {addedMovieRating.Movie.Id}, Название: {addedMovieRating.Movie.Title}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Отсутствуют данные о фильме.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить отзыв.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при добавлении отзыва:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test UpdateMovieRating НЕ НУЖНО
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для обновления отзыва...");

        //    // Создаём объект с обновлёнными данными
        //    var updatedMovieRating = new MovieRating
        //    {
        //        MoviesId = 2,  // ID фильма
        //        ReviewText = "Its my perfect victory"  // Обновленный текст отзыва
        //    };

        //    // Выполняем запрос на обновление отзыва
        //    var result = await apiClient.UpdateMovieRating(2, updatedMovieRating);

        //    // Проверяем, что отзыв был обновлён
        //    if (result != null)
        //    {
        //        Console.WriteLine("Отзыв успешно обновлён.");
        //        Console.WriteLine($"ID отзыва: {result.Id}");

        //        // Выводим текст отзыва
        //        if (!string.IsNullOrEmpty(result.ReviewText))
        //        {
        //            Console.WriteLine($"Обновленный текст отзыва: {result.ReviewText}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Текст отзыва не указан.");
        //        }

        //        // Выводим дату обновления
        //        Console.WriteLine($"Дата обновления: {result.UpdatedAt}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось обновить отзыв.");
        //    }
        //}
        //catch (Exception ex)
        //{

        //    Console.WriteLine("Произошла ошибка при обновлении отзыва:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion





        #region Test GetStudios
        try
        {
            Console.WriteLine("Начинаем запрос для получения списка студий...");

            var studios = await apiClient.GetStudios();

            if (studios != null && studios.Any())
            {
                Console.WriteLine($"Получено студий: {studios.Count}");
                foreach (var studio in studios)
                {
                    Console.WriteLine($"ID: {studio.Id}, Название: {studio.Name}, Фильмов в студии: {studio.Movies.Count}");
                }
            }
            else
            {
                Console.WriteLine("Список студий пуст или не удалось получить данные.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Произошла ошибка при получении списка студий:");
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        }
        #endregion

        #region Test GetStudioById
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для получения студии по ID...");

        //    int studioId = 1; // Указываем ID студии
        //    var studio = await apiClient.GetStudioById(studioId);

        //    if (studio != null)
        //    {
        //        Console.WriteLine($"Студия получена успешно: ID: {studio.Id}, Название: {studio.Name}, Количество фильмов: {studio.Movies.Count}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Студия с ID {studioId} не найдена.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка при получении студии по ID:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test DeleteStudio
        //try
        //{
        //    Console.WriteLine("Начинаем запрос на удаление студии...");

        //    int studioId = 4; // Указываем ID студии, которую хотим удалить
        //    Console.WriteLine($"Попытка удалить студию с ID: {studioId}");

        //    bool isDeleted = await apiClient.DeleteStudio(studioId);

        //    if (isDeleted)
        //    {
        //        Console.WriteLine($"Студия с ID {studioId} успешно удалена.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось удалить студию с ID {studioId}. Возможно, студии не существует.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка при удалении студии:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test AddStudio
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления новой студии...");

        //    var newStudio = new Studio
        //    {
        //        Name = "Fantastic Films"
        //    };

        //    // Выполняем запрос на добавление студии
        //    var addedStudio = await apiClient.AddStudio(newStudio);

        //    // Проверяем, что студия была добавлена
        //    if (addedStudio != null)
        //    {
        //        Console.WriteLine("Студия успешно добавлена.");
        //        Console.WriteLine($"ID: {addedStudio.Id}, Название: {addedStudio.Name}");

        //        // Выводим дополнительные данные, если они существуют
        //        if (addedStudio.CreatedAt.HasValue)
        //        {
        //            Console.WriteLine($"Дата создания: {addedStudio.CreatedAt.Value:yyyy-MM-dd}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Дата создания не указана.");
        //        }

        //        if (addedStudio.UpdatedAt.HasValue)
        //        {
        //            Console.WriteLine($"Дата обновления: {addedStudio.UpdatedAt.Value:yyyy-MM-dd}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Дата обновления не указана.");
        //        }

        //        // Проверяем и выводим список фильмов, если они есть
        //        if (addedStudio.Movies.Any())
        //        {
        //            Console.WriteLine("Фильмы в студии:");
        //            foreach (var movie in addedStudio.Movies)
        //            {
        //                Console.WriteLine($"ID: {movie.Id}, Название: {movie.Title}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("В студии нет фильмов.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить студию.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при добавлении студии:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test UpdateStudio
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для обновления студии...");


        //    var updatedStudio = new Studio
        //    {
        //        Id = 1,  // Идентификатор студии для обновления
        //        Name = "Updated Studio Name", // Новое имя студии
        //        UpdatedAt = DateTime.UtcNow, // Обновлённая дата
        //        CreatedAt = DateTime.UtcNow, // Если нужна дата создания
        //        Movies = new List<Movie>()  // Можете добавить фильмы, если нужно
        //    };

        //    // Выполняем запрос на обновление студии
        //    var updated = await apiClient.UpdateStudio(1, updatedStudio);

        //    // Проверяем, что студия была обновлена
        //    if (updated != null)
        //    {
        //        Console.WriteLine($"Студия успешно обновлена. Имя: {updated.Name}");
        //        Console.WriteLine($"ID студии: {updated.Id}");

        //        // Выводим информацию о фильмах, если есть
        //        if (updated.Movies.Any())
        //        {
        //            Console.WriteLine("Фильмы студии:");
        //            foreach (var movie in updated.Movies)
        //            {
        //                Console.WriteLine($"Фильм: {movie.Title}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("Нет фильмов для этой студии.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось обновить студию.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при обновлении студии:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion



        #region Test GetGenres
        //try
        //{
        //    Console.WriteLine("Запрос на получение всех жанров...");

        //    var genres = await apiClient.GetGenres();

        //    if (genres != null && genres.Any())
        //    {
        //        Console.WriteLine($"Жанры получены. Всего жанров: {genres.Count}");
        //        foreach (var genre in genres)
        //        {
        //            Console.WriteLine($"ID: {genre.Id}, Название: {genre.Name}");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Жанры не найдены.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Ошибка при получении жанров:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test GetGenreById
        //try
        //{
        //    int genreId = 1; // Пример ID жанра
        //    Console.WriteLine($"Запрос на получение жанра с ID {genreId}...");

        //    var genre = await apiClient.GetGenreById(genreId);

        //    if (genre != null)
        //    {
        //        Console.WriteLine("Жанр получен успешно.");
        //        Console.WriteLine($"ID: {genre.Id}, Название: {genre.Name}");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Жанр с ID {genreId} не найден.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Ошибка при получении жанра:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test DeleteGenre
        //try
        //{
        //    int genreId = 1; // Пример ID жанра для удаления
        //    Console.WriteLine($"Запрос на удаление жанра с ID {genreId}...");

        //    bool isDeleted = await apiClient.DeleteGenre(genreId);

        //    if (isDeleted)
        //    {
        //        Console.WriteLine($"Жанр с ID {genreId} успешно удален.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Не удалось удалить жанр с ID {genreId}. Возможно, жанра не существует.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Ошибка при удалении жанра:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test AddGenre Работает, должны быть уникальные названия для жанра
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления нового жанра...");


        //    var newGenre = new Genre
        //    {
        //        Name = "New Genre"  
        //    };


        //    var addedGenre = await apiClient.AddGenre(newGenre);

        //    if (addedGenre != null)
        //    {
        //        Console.WriteLine("Жанр успешно добавлен.");
        //        Console.WriteLine($"ID: {addedGenre.Id}, Название: {addedGenre.Name}");

        //        if (addedGenre.CreatedAt.HasValue)
        //        {
        //            Console.WriteLine($"Дата создания: {addedGenre.CreatedAt.Value:yyyy-MM-dd}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Дата создания не указана.");
        //        }

        //        if (addedGenre.UpdatedAt.HasValue)
        //        {
        //            Console.WriteLine($"Дата обновления: {addedGenre.UpdatedAt.Value:yyyy-MM-dd}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Дата обновления не указана.");
        //        }

        //        // Проверяем и выводим список фильмов, если они есть
        //        if (addedGenre.Movies?.Any() == true)
        //        {
        //            Console.WriteLine("Фильмы в жанре:");
        //            foreach (var movie in addedGenre.Movies)
        //            {
        //                Console.WriteLine($"ID: {movie.Id}, Название: {movie.Title}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("В жанре нет фильмов.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить жанр.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при добавлении жанра:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion

        #region Test UpdateGenre
        //try
        //{
        //    Console.WriteLine("Начинаем запрос для обновления жанра...");

        //    // Создаём объект Genre с обновлёнными данными
        //    var updatedGenre = new Genre
        //    {
        //        Id = 6,  // Идентификатор жанра для обновления
        //        Name = "Updated Genre Name", // Новое имя жанра
        //        UpdatedAt = DateTime.UtcNow, // Обновлённая дата
        //        CreatedAt = DateTime.UtcNow, // Если нужна дата создания
        //        Movies = new List<Movie>()  // Можете добавить фильмы, если нужно
        //    };

        //    // Выполняем запрос на обновление жанра
        //    var updated = await apiClient.UpdateGenre(updatedGenre.Id, updatedGenre);

        //    // Проверяем, что жанр был обновлен
        //    if (updated != null)
        //    {
        //        Console.WriteLine($"Жанр успешно обновлён. Имя: {updated.Name}");
        //        Console.WriteLine($"ID жанра: {updated.Id}");

        //        // Выводим информацию о фильмах, если есть
        //        if (updated.Movies.Any())
        //        {
        //            Console.WriteLine("Фильмы жанра:");
        //            foreach (var movie in updated.Movies)
        //            {
        //                Console.WriteLine($"Фильм: {movie.Title}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("Нет фильмов для этого жанра.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось обновить жанр.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    // Логируем ошибку
        //    Console.WriteLine("Произошла ошибка при обновлении жанра:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");

        //    // Дополнительный вывод стека ошибок для диагностики
        //    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
        //}
        #endregion



        #region Test GetFavoriteMovies

        //try
        //{
        //    Console.WriteLine("Начинаем запрос для получения любимых фильмов...");

        //    var favoriteMovies = await apiClient.GetFavoriteMovies(testToken);

        //    if (favoriteMovies != null && favoriteMovies.Any())
        //    {
        //        Console.WriteLine("Список любимых фильмов:");
        //        foreach (var movie in favoriteMovies)
        //        {
        //            Console.WriteLine($"ID: {movie.Id}");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Нет любимых фильмов или ошибка при получении данных.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //}

        #endregion

        #region Test RemoveMovieFromFavorites
        //int movieIdToRemove = 2;  

        //try
        //{
        //    Console.WriteLine("Начинаем запрос для удаления фильма из избранного...");

        //    // Выполняем запрос на удаление фильма
        //    bool isRemoved = await apiClient.RemoveMovieFromFavorites(movieIdToRemove, testToken);

        //    if (isRemoved)
        //    {
        //        Console.WriteLine("Фильм был успешно удалён из избранного.");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось удалить фильм из избранного.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //}


        #endregion

        #region Test AddMovieToFavorites
        //int movieIdToAdd = 2;  

        //try
        //{
        //    Console.WriteLine("Начинаем запрос для добавления фильма в избранное...");

        //    // Выполняем запрос на добавление фильма
        //    bool isAdded = await apiClient.AddMovieToFavorites(movieIdToAdd, testToken);

        //    if (isAdded)
        //    {
        //        Console.WriteLine("Фильм был успешно добавлен в избранное.");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Не удалось добавить фильм в избранное.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("Произошла ошибка:");
        //    Console.WriteLine($"Ошибка: {ex.Message}");
        //}
        #endregion

    }

}
