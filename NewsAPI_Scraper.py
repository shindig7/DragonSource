import re
import requests
import json
from lxml import html
import os


API_KEY = "1737d6ed96664682ba73f616084ec3e9"

def main():
    global API_KEY
    site_list = source_from_json()
    print site_list
    for source in site_list:
        content_list = []
        url = 'https://newsapi.org/v1/articles?source=%s&apiKey=%s' % (source, API_KEY)
        url_list = article_from_json(url)
        for link in url_list:
            try:
                page = requests.get(link)
                tree = html.fromstring(page.content)
                words = tree.xpath('//p/text()')
                main_story = [x.encode('ascii', 'ignore').encode('utf-8') for x in words if len(x) > 100]
                print '%s has been processed' % link
                content_list.extend(main_story)
            except:
                pass
        print(content_list)
        output_file(source, content_list)

def article_from_json(link):
    url_list = []
    page = requests.get(link)
    js = json.loads(page.content)
    for articles in js['articles']:
        url_list.append(articles['url'].encode('ascii', 'ignore'))
    return url_list    
    

def source_from_json():
    source_list = []
    api_url = 'https://newsapi.org/v1/sources?language=en'
    page = requests.get(api_url)
    json_file = json.loads(page.content)
    for source in json_file['sources']:
        source_list.append(source['id'].encode('ascii', 'ignore'))
    return source_list

def output_file(name, content):
    file_name = '%s.txt' % name
    file_new = open(file_name, 'w')
    file_new.write(name)
    file_new.write('\n')
    file_new.close()
    file_append = open(file_name, 'a')
    for x in content: file_append.write(x) 
    file_append.close()


main()
