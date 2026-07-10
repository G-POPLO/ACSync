# ACSync
特点：

- 极其简单的增量更新系统，任何二进制文件都可以使用
- 补丁为新版本更新的差异文件，压缩至7z格式

- 允许打补丁时删除无用的旧文件
- 允许打补丁时排除文件（例如json、ini、yaml等）
- 使用LZMA2压缩算法

流程：

- 创建清单：acsync <path> -l

文件清单包含目录全部文件名、相对路径，及其修改时间，JSON格式

- 更新：acsync <path> -u

打补丁（解压文件）

- 创建补丁:acsync <oldpath>  <newpath> -m

更新先读取新版本目录创建JSON清单，然后读取旧版本清单，比对两份清单差异，选择不同文本进行压缩制成补丁

命令：

```md
创建清单：acsync <path> -l
更新：acsync <path> -u
创建补丁:acsync <oldpath>  <newpath> -m
```