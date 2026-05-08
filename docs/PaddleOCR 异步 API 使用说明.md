# PaddleOCR 异步 API 使用说明
来源：[PaddleOCR 异步 API 使用说明](https://ai.baidu.com/ai-doc/AISTUDIO/fml7mozw5)

## [](#%E5%BC%82%E6%AD%A5-api-%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E)异步 API 使用说明

+   单次请求最大支持 1000 页 pdf
+   支持传入文件链接。文件大小不能超过 200 MB
+   支持上传本地文件。文件大小不能超过 50 MB

## [](#%E5%BC%82%E6%AD%A5-api-%E5%AE%8C%E6%95%B4%E8%B0%83%E7%94%A8%E7%A4%BA%E4%BE%8B)异步 API 完整调用示例

针对不同的模型，返回结果的字段略有差异。以下分别提供 **PaddleOCR-VL 系列 / PP-StructureV3** 和 **PP-OCRv5** 的调用示例。

## [](#1-paddleocr-vl-15%E3%80%81paddleocr-vl%E3%80%81pp-structurev3-%E8%B0%83%E7%94%A8%E7%A4%BA%E4%BE%8B)1\. PaddleOCR-VL-1.5、PaddleOCR-VL、PP-StructureV3 调用示例

适用于 `PaddleOCR-VL-1.5`、`PaddleOCR-VL` 和 `PP-StructureV3` 模型。

```python
# Please make sure the requests library is installed
# pip install requests
import json
import os
import requests
import sys
import time

JOB_URL = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs"
TOKEN = ""
# 可选模型: "PaddleOCR-VL-1.5", "PaddleOCR-VL", "PP-StructureV3"
MODEL = "PaddleOCR-VL-1.5"

file_path = "<local file path or file url>"

headers = {
    "Authorization": f"bearer {TOKEN}",
}

optional_payload = {
    "useDocOrientationClassify": False,
    "useDocUnwarping": False,
    "useChartRecognition": False,
}

print(f"Processing file: {file_path}")

if file_path.startswith("http"):
    # URL Mode
    headers["Content-Type"] = "application/json"
    payload = {
        "fileUrl": file_path,
        "model": MODEL,
        "optionalPayload": optional_payload
    }
    job_response = requests.post(JOB_URL, json=payload, headers=headers)
else:
    # Local File Mode
    if not os.path.exists(file_path):
        print(f"Error: File not found at {file_path}")
        sys.exit(1)
        
    data = {
        "model": MODEL,
        "optionalPayload": json.dumps(optional_payload)
    }
    
    with open(file_path, "rb") as f:
        files = {"file": f}
        job_response = requests.post(JOB_URL, headers=headers, data=data, files=files)

print(f"Response status: {job_response.status_code}")
if job_response.status_code != 200:
    print(f"Response content: {job_response.text}")

assert job_response.status_code == 200
jobId = job_response.json()["data"]["jobId"]
print(f"Job submitted successfully. job id: {jobId}")
print("Start polling for results")

jsonl_url = ""
while True:
    job_result_response = requests.get(f"{JOB_URL}/{jobId}", headers=headers)
    assert job_result_response.status_code == 200
    state = job_result_response.json()["data"]["state"]
    if state == 'pending':
        print("The current status of the job is pending")
    elif state == 'running':
        try:
            total_pages = job_result_response.json()['data']['extractProgress']['totalPages']
            extracted_pages = job_result_response.json()['data']['extractProgress']['extractedPages']
            print(f"The current status of the job is running, total pages: {total_pages}, extracted pages: {extracted_pages}")
        except KeyError:
             print("The current status of the job is running...")
    elif state == 'done':
        extracted_pages = job_result_response.json()['data']['extractProgress']['extractedPages']
        start_time = job_result_response.json()['data']['extractProgress']['startTime']
        end_time = job_result_response.json()['data']['extractProgress']['endTime']
        print(f"Job completed, successfully extracted pages: {extracted_pages}, start time: {start_time}, end time: {end_time}")
        jsonl_url = job_result_response.json()['data']['resultUrl']['jsonUrl']
        break
    elif state == "failed":
        error_msg = job_result_response.json()['data']['errorMsg']
        print(f"Job failed, failure reason：{error_msg}")
        sys.exit()

    time.sleep(5)

if jsonl_url:
    jsonl_response = requests.get(jsonl_url)
    jsonl_response.raise_for_status()
    lines = jsonl_response.text.strip().split('\n')
    output_dir = "output"
    os.makedirs(output_dir, exist_ok=True)
    page_num = 0
    for line_num, line in enumerate(lines, start=1):
        line = line.strip()
        if not line:
            continue
        result = json.loads(line)["result"]
        # 注意：此处使用的是 layoutParsingResults 字段
        for i, res in enumerate(result["layoutParsingResults"]):
            md_filename = os.path.join(output_dir, f"doc_{page_num}.md")
            with open(md_filename, "w", encoding="utf-8") as md_file:
                md_file.write(res["markdown"]["text"])
            print(f"Markdown document saved at {md_filename}")
            for img_path, img in res["markdown"]["images"].items():
                full_img_path = os.path.join(output_dir, img_path)
                os.makedirs(os.path.dirname(full_img_path), exist_ok=True)
                img_bytes = requests.get(img).content
                with open(full_img_path, "wb") as img_file:
                    img_file.write(img_bytes)
                print(f"Image saved to: {full_img_path}")
            for img_name, img in res["outputImages"].items():
                img_response = requests.get(img)
                if img_response.status_code == 200:
                    # Save image to local
                    filename = os.path.join(output_dir, f"{img_name}_{page_num}.jpg")
                    with open(filename, "wb") as f:
                        f.write(img_response.content)
                    print(f"Image saved to: {filename}")
                else:
                    print(f"Failed to download image, status code: {img_response.status_code}")
            page_num += 1
```

## [](#2-pp-ocrv5-%E8%B0%83%E7%94%A8%E7%A4%BA%E4%BE%8B)2\. PP-OCRv5 调用示例

适用于 `PP-OCRv5` 模型。

```python
# Please make sure the requests library is installed
# pip install requests
import json
import os
import requests
import sys
import time

JOB_URL = "https://paddleocr.aistudio-app.com/api/v2/ocr/jobs"
TOKEN = ""
MODEL = "PP-OCRv5"

file_path = "<local file path or file url>"

headers = {
    "Authorization": f"bearer {TOKEN}",
}

optional_payload = {
    "useDocOrientationClassify": False,
    "useDocUnwarping": False,
    "useTextlineOrientation": False,
}

print(f"Processing file: {file_path}")

if file_path.startswith("http"):
    # URL Mode
    headers["Content-Type"] = "application/json"
    payload = {
        "fileUrl": file_path,
        "model": MODEL,
        "optionalPayload": optional_payload
    }
    job_response = requests.post(JOB_URL, json=payload, headers=headers)
else:
    # Local File Mode
    if not os.path.exists(file_path):
        print(f"Error: File not found at {file_path}")
        sys.exit(1)
        
    data = {
        "model": MODEL,
        "optionalPayload": json.dumps(optional_payload)
    }
    
    with open(file_path, "rb") as f:
        files = {"file": f}
        job_response = requests.post(JOB_URL, headers=headers, data=data, files=files)

print(f"Response status: {job_response.status_code}")

if job_response.status_code != 200:
    print(f"Response content: {job_response.text}")

assert job_response.status_code == 200
jobId = job_response.json()["data"]["jobId"]
print(f"Job submitted successfully. job id: {jobId}")
print("Start polling for results")

jsonl_url = ""
while True:
    job_result_response = requests.get(f"{JOB_URL}/{jobId}", headers=headers)
    assert job_result_response.status_code == 200
    state = job_result_response.json()["data"]["state"]
    if state == 'pending':
        print("The current status of the job is pending")
    elif state == 'running':
        try:
            total_pages = job_result_response.json()['data']['extractProgress']['totalPages']
            extracted_pages = job_result_response.json()['data']['extractProgress']['extractedPages']
            print(f"The current status of the job is running, total pages: {total_pages}, extracted pages: {extracted_pages}")
        except KeyError:
             print("The current status of the job is running...")
    elif state == 'done':
        extracted_pages = job_result_response.json()['data']['extractProgress']['extractedPages']
        start_time = job_result_response.json()['data']['extractProgress']['startTime']
        end_time = job_result_response.json()['data']['extractProgress']['endTime']
        print(f"Job completed, successfully extracted pages: {extracted_pages}, start time: {start_time}, end time: {end_time}")
        jsonl_url = job_result_response.json()['data']['resultUrl']['jsonUrl']
        break
    elif state == "failed":
        error_msg = job_result_response.json()['data']['errorMsg']
        print(f"Job failed, failure reason：{error_msg}")
        sys.exit()

    time.sleep(5)

if jsonl_url:
    jsonl_response = requests.get(jsonl_url)
    jsonl_response.raise_for_status()
    lines = jsonl_response.text.strip().split('\n')
    output_dir = "output"
    os.makedirs(output_dir, exist_ok=True)
    page_num = 0
    for line_num, line in enumerate(lines, start=1):
        line = line.strip()
        if not line:
            continue
        result = json.loads(line)["result"]
        # 注意：PP-OCRv5 使用 ocrResults 字段
        for i, res in enumerate(result["ocrResults"]):
            image_url = res["ocrImage"]
            img_response = requests.get(image_url)
            if img_response.status_code == 200:
                # Save image to local
                filename = f"output/img_output_{page_num}.jpg"
                with open(filename, "wb") as f:
                    f.write(img_response.content)
                print(f"Image saved to: {filename}")
            else:
                print(f"Failed to download image, status code: {img_response.status_code}")
            page_num += 1
```

## [](#%E6%8E%A5%E5%8F%A3%E6%96%87%E6%A1%A3)接口文档

Base URL：[https://paddleocr.aistudio-app.com/](https://paddleocr.aistudio-app.com/)

## [](#%E6%8F%90%E4%BA%A4%E8%A7%A3%E6%9E%90%E4%BB%BB%E5%8A%A1)提交解析任务

**Path: /api/v2/ocr/jobs**

**Method: POST**

**Header:**

+   Authorization: Bearer {access\_token}
+   Content-Type: application/json（传入文件链接时设置，示例代码中已自动适配）
+   Content-Type: multipart/form-data（上传文件时设置，示例代码中已自动适配）
+   Accept-Encoding: gzip, deflate, br

### [](#%E8%AF%B7%E6%B1%82%E5%8F%82%E6%95%B0%E8%AF%B4%E6%98%8E)请求参数说明

| 参数 | 类型 | 是否必填 | 示例 | 描述 |
| --- | --- | --- | --- | --- |
| `file` | `bytes` | 是（与 fileUrl 二选一） |  | 二进制文件数据 |
| `fileUrl` | `string` | 是（与 file 二选一） |  | 文件链接 |
| `model` | `string` | 是 | PP-OCRv5  
PP-StructureV3  
PaddleOCR-VL  
PaddleOCR-VL-1.5 | OCR 模型名 |
| `optionalPayload` | `object` | 否 | {"useDocOrientationClassify": false} | 解析参数，不同模型类型不同，参考：  
PP-OCRV5：[文档](https://ai.baidu.com/ai-doc/AISTUDIO/Kmfl2ycs0)  
PP-StructureV3：[文档](https://ai.baidu.com/ai-doc/AISTUDIO/Fmfz6oh2e)  
PaddleOCR-VL：[文档](https://ai.baidu.com/ai-doc/AISTUDIO/2mh4okm66)  
PaddleOCR-VL-1.5：[文档](https://ai.baidu.com/ai-doc/AISTUDIO/2mh4okm66) |
| `pageRanges` | `string` | 否 | "2,4-6"：第2页，第4到第6页  
"2--2"：第2页到倒数第2页 | 指定解析的页码范围 |
| `batchId` | `string` | 否 | 可唯一标识的字符串 | 批量ID，用于批量查询任务 |

### [](#%E5%93%8D%E5%BA%94%E5%8F%82%E6%95%B0%E8%AF%B4%E6%98%8E)响应参数说明

| 参数 | 类型 | 示例 | 说明 |
| --- | --- | --- | --- |
| `traceId` | `string` | 0b1eb3150f5bec03dab9e74b4264c615 | 请求 ID |
| `code` | `int` | 10002 | 接口状态码，成功为 0，失败详情参考“错误码说明” |
| `msg` | `string` | 文件 URL 无法识别 | 接口响应信息，失败详情参考“错误码说明” |
| `data` | `object` |  |  |
| `data.jobId` | `string` | ocrjob-f4377241b695 | 任务 ID |

## [](#%E8%8E%B7%E5%8F%96%E8%A7%A3%E6%9E%90%E7%BB%93%E6%9E%9C)获取解析结果

**Path: /api/v2/ocr/jobs/{jobId}**

**Method: GET**

**Header:**

+   Authorization: Bearer {access\_token}
+   Content-Type: application/json

### [](#%E5%93%8D%E5%BA%94%E5%8F%82%E6%95%B0%E8%AF%B4%E6%98%8E-1)响应参数说明

| 参数 | 类型 | 示例 | 说明 |
| --- | --- | --- | --- |
| `traceId` | `string` | 0b1eb3150f5bec03dab9e74b4264c615 | 请求 ID |
| `code` | `int` | 0 | 接口状态码，成功：0 |
| `msg` | `string` | Success | 接口处理信息，成功："Success" |
| `data` | `object` |  |  |
| `data.jobId` | `string` | ocrjob-f4377241b695 | 任务 ID |
| `data.state` | `string` | done | 任务处理状态  
\* done：完成  
\* pending：排队中  
\* running：正在解析中  
\* failed：解析失败（不存在部分页成功的情况） |
| `data.errorMsg` | `string` | 文件格式不支持，请上传符合要求的文件类型 | 解析失败原因 state=failed 时该值有效 |
| `data.resultUrl` | `object` | 提供 BOS 短链  
{ "jsonUrl": "https://\*\*\*.com", "markdownUrl": "https://\*\*\*.com"} | 文档解析结果 state=done 时该值有效 |
| `data.extractProgress` | `object` |  | 文档解析进度 state=running 时该值有效 |
| `data.extractProgress.startTime` | `string` | 2026-01-01T12:00:00+08:00 | 文档解析开始时间 |
| `data.extractProgress.endTime` | `string` | 2026-01-01T12:00:00+08:00 | 文档解析结束时间 |
| `data.extractProgress.totalPages` | `string` | 10 | 文档总页数 |
| `data.extractProgress.extractedPages` | `string` | 1 | 文档已解析页数 |

## [](#%E6%89%B9%E9%87%8F%E8%8E%B7%E5%8F%96%E4%BB%BB%E5%8A%A1%E7%BB%93%E6%9E%9C)批量获取任务结果

**Path: /api/v2/ocr/jobs/batch/{batchId}**

**Method: GET**

**Header:**

+   Authorization: Bearer {access\_token}
+   Content-Type: application/json
+   Accept-Encoding: gzip, deflate, br

**响应参数说明**

| 参数 | 类型 | 示例 | 说明 |
| --- | --- | --- | --- |
| `traceId` | `string` | 0b1eb3150f5bec03dab9e74b4264c615 | 请求 ID |
| `code` | `int` | 0 | 接口状态码，成功：0 |
| `msg` | `string` | Success | 接口处理信息，成功："Success" |
| `data` | `object` |  |  |
| `data.batchId` | `string` | batchid-202601210000 | 批量任务 ID，用户自定义传入，形式自订。 |
| `data.extractResult` | `array` |  | 推理结果列表 |
| `data.extractResult.jobId` | `string` | ocrjob-f4377241b695 | 任务 ID |
| `data.extractResult.state` | `string` | done | 任务处理状态  
\* done：完成  
\* pending：排队中  
\* running：正在解析中  
\* failed：解析失败（不存在部分页成功的情况） |
| `data.extractResult.errorMsg` | `string` | 文件格式不支持，请上传符合要求的文件类型 | 解析失败原因 state=failed 时该值有效 |
| `data.extractResult.resultUrl` | `object` | 提供 BOS 短链  
{ "jsonUrl": "https://\*\*\*.com", "markdownUrl": "https://\*\*\*.com"} | 文档解析结果 state=done 时该值有效 |
| `data.extractResult.extractProgress` | `object` |  | 文档解析进度 state=running 时该值有效 |
| `data.extractResult.extractProgress.startTime` | `string` | 2026-01-01T12:00:00+08:00 | 文档解析开始时间 |
| `data.extractResult.extractProgress.endTime` | `string` | 2026-01-01T12:00:00+08:00 | 文档解析结束时间 |
| `data.extractResult.extractProgress.totalPages` | `int` | 10 | 文档总页数 |
| `data.extractResult.extractProgress.extractedPages` | `int` | 1 | 文档已解析页数 |

## [](#%E9%94%99%E8%AF%AF%E7%A0%81)错误码

| 错误码 | 错误码说明 | 解决建议 |
| --- | --- | --- |
| 401 | Token 无效 | 检查token，从链接中获取token：[链接](https://aistudio.baidu.com/account/accessToken) |
| 500 | 系统错误 | 请联系官方，或稍后重试 |
| 10001 | 空文件（http status 对应为 400） | 请检查文件 |
| 10002 | 文件 URL 无法识别（http status 对应为 400） | 请检查URL |
| 10003 | 文件大小超出限制（http status 对应为 400） | 请检查文件 |
| 10004 | 文件格式不支持（http status 对应为 400） | 请检查文件 |
| 10005 | 文件内容无法解析（http status 对应为 400） | 请检查文件 |
| 10006 | 文件页数超过限制（http status 对应为 400） | 请检查文件 |
| 10007 | 模型参数错误（http status 对应为 400） | 模型不存在，请检查模型名 |
| 10008 | 请求参数错误（http status 对应为 400） | optionalPayload或extraFormats参数错误，请根据data.errorMsg提示修正 |
| 10009 | 同一 batchId 的任务仅允许创建 100 条（http status 对应为 400） | 请更换batchId |
| 10010 | 任务提交队列已满 | 请稍后重试 |
| 11001 | jobId 不存在（http status 对应为 404） | 请检查jobId |
| 11002 | job 已过期（http status 对应为 400） | 请更换JobId |
| 11003 | job 解析失败（http status 对应为 200） | 解析失败，具体原因见：data.errorMsg |
| 12001 | 已达每日页数上限（http status 对应为 403） | 超过上每日限额，如需提升请参看[配额说明](https://ai.baidu.com/ai-doc/AISTUDIO/Xmjclapam) |
| 12002 | 请求频率过高（http status 对应为 429） | 请稍后重试 |