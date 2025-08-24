using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterCLS
{

    public class CommentCsvParser
    {
        private readonly string _folderPath;

        public CommentCsvParser(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"指定的文件夹不存在: {folderPath}");

            _folderPath = folderPath;
        }
        //实例构造函数

        public List<Comment> GetCommentsByTeacherId(string teacherId)
        {
            var results = new List<Comment>();
            var csvFiles = Directory.GetFiles(_folderPath, "comment_*.csv");

            foreach (var filePath in csvFiles)
            {
                try
                {
                    var fileComments = ParseCsvFile(filePath,teacherId);
                    results.AddRange(fileComments);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理文件 {Path.GetFileName(filePath)} 时出错: {ex.Message}");
                }
            }

            return results;
        }

        private List<Comment> ParseCsvFile(string filePath, string teacherId)
        {
            var comments = new List<Comment>();
            var lines = File.ReadAllLines(filePath);

            // 跳过表头行
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = ParseCsvLine(line);

                if (fields.Length < 8)
                {
                    Console.WriteLine($"跳过格式不正确的行: {line}");
                    continue;
                }

                // 检查教师姓名是否匹配（精确匹配）
                if (fields[1] != teacherId)
                    continue;

                try
                {
                    var comment = new Comment
                    {
                        CommentId = long.Parse(fields[0]),
                        TeacherId = long.Parse(fields[1]),
                        TeacherName = fields[2],
                        PublishTime = DateTime.Parse(fields[3]),
                        LikeMinusDislike = int.Parse(fields[4]),
                        LikeCount = int.Parse(fields[5]),
                        DislikeCount = int.Parse(fields[6]),
                        Content = fields[7].Replace("\\n","\n")
                    };

                    comments.Add(comment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析行时出错: {line}. 错误: {ex.Message}");
                }
            }

            return comments;
        }

        private string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
                    {
                        // 处理转义引号 ("")
                        currentField.Append('"');
                        i++; // 跳过下一个引号
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields.ToArray();
        }
    }
    public class ProfileParser
    {
        private readonly string _folderPath;
        private List<TeacherProfile> _allProfiles;

        public ProfileParser(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"指定的文件夹不存在: {folderPath}");

            _folderPath = folderPath;
            _allProfiles = new List<TeacherProfile>();

            // 初始化时加载所有教师信息
            LoadAllProfiles();
        }

        /// <summary>
        /// 从 teachers.csv 文件加载所有教师信息
        /// </summary>
        private void LoadAllProfiles()
        {
            string filePath = Path.Combine(_folderPath, "teachers.csv");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"找不到 teachers.csv 文件: {filePath}");

            try
            {
                var lines = File.ReadAllLines(filePath);         
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var profile = ParseProfileLine(line);
                    if (profile != null)
                    {
                        _allProfiles.Add(profile);
                    }
                }

                Console.WriteLine($"成功加载 {_allProfiles.Count} 条教师信息");
            }
            catch (Exception ex)
            {
                throw new Exception($"加载教师信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析单行教师信息
        /// </summary>
        private TeacherProfile ParseProfileLine(string line)
        {
            var fields = ParseCsvLine(line);

            if (fields.Length < 8)
            {
                Console.WriteLine($"跳过格式不正确的行: {line}");
                return null;
            }

            try
            {
                return new TeacherProfile
                {
                    Id = int.Parse(fields[0]),
                    Name = fields[1],
                    College = fields[2],
                    Heat = int.Parse(fields[3]),
                    RaterCount = int.Parse(fields[4]),
                    Rating = double.Parse(fields[5]),
                    Pinyin = fields[6],
                    Abbreviation = fields[7]
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析教师信息行时出错: {line}. 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查找姓名包含指定字符串的所有教师
        /// </summary>
        /// <param name="namePart">姓名部分字符串</param>
        /// <returns>匹配的教师信息列表</returns>
        public List<TeacherProfile> FindTeachersByName(string namePart)
        {
            if (string.IsNullOrWhiteSpace(namePart))
                return new List<TeacherProfile>();

            return _allProfiles
                .Where(p => p.Name.Contains(namePart, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 查找指定ID的教师
        /// </summary>
        public TeacherProfile FindTeacherById(int id)
        {
            return _allProfiles.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// 查找指定学院的教师
        /// </summary>
        public List<TeacherProfile> FindTeachersByCollege(string college)
        {
            return _allProfiles
                .Where(p => p.College.Equals(college, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 获取所有教师信息
        /// </summary>
        public List<TeacherProfile> GetAllTeachers()
        {
            return new List<TeacherProfile>(_allProfiles);
        }

        /// <summary>
        /// 解析CSV行，处理可能的引号包围字段
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
                    {
                        // 处理转义引号 ("")
                        currentField.Append('"');
                        i++; // 跳过下一个引号
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields.ToArray();
        }

        /// <summary>
        /// 检查字符串是否为数字
        /// </summary>
        private bool IsNumeric(string value)
        {
            return int.TryParse(value, out _);
        }
    }
    public class GpaJsonParser
    {
        private readonly string _folderPath;
        private Dictionary<string, List<List<object>>> _gpaData;

        public GpaJsonParser(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"指定的文件夹不存在: {folderPath}");

            _folderPath = folderPath;
            _gpaData = new Dictionary<string, List<List<object>>>();

            // 初始化时加载所有GPA数据
            LoadGpaData();
        }

        /// <summary>
        /// 从 gpa.json 文件加载所有GPA数据
        /// </summary>
        private void LoadGpaData()
        {
            string filePath = Path.Combine(_folderPath, "gpa.json");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"找不到 gpa.json 文件: {filePath}");

            try
            {
                string jsonContent = File.ReadAllText(filePath);

                // 使用 System.Text.Json 解析JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _gpaData = JsonSerializer.Deserialize<Dictionary<string, List<List<object>>>>(jsonContent, options);

                Console.WriteLine($"成功加载 {_gpaData.Count} 位教师的GPA数据");
            }
            catch (Exception ex)
            {
                throw new Exception($"加载GPA数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据教师姓名获取该教师的所有课程GPA信息
        /// </summary>
        /// <param name="teacherName">教师姓名</param>
        /// <returns>该教师的所有课程GPA信息列表</returns>
        public List<CourseGpaInfo> GetCoursesByTeacher(string teacherName)
        {
            var result = new List<CourseGpaInfo>();

            // 查找匹配的教师（不区分大小写）
            var matchingTeachers = _gpaData
                .Where(kvp => kvp.Key.Equals(teacherName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var teacher in matchingTeachers)
            {
                foreach (var courseData in teacher.Value)
                {
                    if (courseData.Count < 4)
                    {
                        Console.WriteLine($"跳过格式不正确的课程数据: {string.Join(", ", courseData)}");
                        continue;
                    }

                    try
                    {
                        var courseInfo = new CourseGpaInfo
                        {
                            CourseName = courseData[0]?.ToString() ?? "",
                            AverageGpa = Convert.ToDouble(courseData[1]?.ToString()),
                            StudentCount = courseData[2]?.ToString()??"无数据",
                            StandardDeviation = Convert.ToDouble(courseData[3]?.ToString()),
                            TeacherName = teacher.Key
                        };

                        result.Add(courseInfo);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"解析课程数据时出错: {string.Join(", ", courseData)}. 错误: {ex.Message}");
                        throw;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 根据课程名获取所有教师在该课程的GPA信息
        /// </summary>
        /// <param name="courseName">课程名</param>
        /// <returns>所有教师在该课程的GPA信息列表</returns>
        public List<CourseGpaInfo> GetTeachersByCourse(string courseName)
        {
            var result = new List<CourseGpaInfo>();

            foreach (var teacher in _gpaData)
            {
                foreach (var courseData in teacher.Value)
                {
                    if (courseData.Count < 4)
                    {
                        Console.WriteLine($"跳过格式不正确的课程数据: {string.Join(", ", courseData)}");
                        continue;
                    }

                    // 检查课程名是否匹配（不区分大小写）
                    if (!courseData[0]?.ToString()?.Equals(courseName, StringComparison.OrdinalIgnoreCase) ?? false)
                        continue;

                    try
                    {
                        var courseInfo = new CourseGpaInfo
                        {
                            CourseName = courseData[0]?.ToString() ?? "",
                            AverageGpa = Convert.ToDouble(courseData[1]?.ToString()),
                            StudentCount = courseData[2]?.ToString()??"无数据",
                            StandardDeviation = Convert.ToDouble(courseData[3]?.ToString()),
                            TeacherName = teacher.Key
                        };

                        result.Add(courseInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析课程数据时出错: {string.Join(", ", courseData)}. 错误: {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有GPA数据
        /// </summary>
        public Dictionary<string, List<List<object>>> GetAllGpaData()
        {
            return _gpaData;
        }
    }
    public static class ValidationHelper
    {
        public static string GetToken(ApplicationDataContainer container, string key)
        {
            if (container.Values.TryGetValue(key, out var token))
            {
                if (token != null)
                {
                    var _token = token.ToString();
                    if (!string.IsNullOrEmpty(_token))
                    {
                        return _token;
                    }
                }
            }
            return "0";
        }
    }
    public class Comment
    {
        public long CommentId { get; set; }
        public long TeacherId { get; set; }
        public required string TeacherName { get; set; }
        public DateTime PublishTime { get; set; }
        public int LikeMinusDislike { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public required string Content { get; set; }

        public override string ToString()
        {
            return $"{TeacherName} ({PublishTime:yyyy-MM-dd HH:mm:ss}): {Content}";
        }
    }
    public class TeacherProfile
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string College { get; set; }
        public int Heat { get; set; } // 热度
        public int RaterCount { get; set; } // 评分人数
        public double Rating { get; set; } // 评分
        public required string Pinyin { get; set; } // 拼音
        public required string Abbreviation { get; set; } // 拼音缩写

        public override string ToString()
        {
            return $"{Name} ({College}) - 评分: {Rating} (基于 {RaterCount} 人评价)";
        }
    }
    public class CourseGpaInfo
    {
        public required string CourseName { get; set; }
        public double AverageGpa { get; set; }
        public required string StudentCount { get; set; }
        public double StandardDeviation { get; set; }
        public required string TeacherName { get; set; } // 添加教师姓名属性，便于第二个函数使用

        public override string ToString()
        {
            return $"{CourseName} - 教师: {TeacherName}, 均绩: {AverageGpa:F2}, 学生数: {StudentCount}, 标准差: {StandardDeviation:F2}";
        }
    }
    public class SearchResult
    {
        public required string Name { get; set; }
        public int Id { get; set; }
        public required string College { get; set; }

    }
}
