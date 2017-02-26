import re
import xml.etree.cElementTree as ET
from lxml import html
import requests
from urlparse import urlparse

API_KEY = '16f12887c8dab2166c6bec9f62ccc3030342a016'


def main():
    url_list=[]
    # The original query URL. As a free user of the API, I was not allowed to dynamically call the API more than the set limit
    # url = 'https://access.alchemyapi.com/calls/data/GetNews?apikey=16f12887c8dab2166c6bec9f62ccc3030342a016&return=enriched.url.url&start=1487462400&end=1488150000&q.enriched.url.enrichedTitle.taxonomy.taxonomy_.label=law,%20govt%20and%20politics&count=100&outputMode=json'
    tree = ET.parse('C:\Users\eagle\Documents\GitHub\DragonSource\AlchemyXMLResult(1).txt')
    root = tree.getroot()
    for x in xrange(987):
        try:
            url = root[3][0][x][1][0][0][0].text[1:-1]
            print url
            url_list.append(url)
        except IndexError:
            print 'Index Error at Element %d' % x
    print url_list
    for x in url_list: get_content(x)


def get_content(link):
    try:
        page = requests.get(link)
        print '%s seen' % link
        tree = html.fromstring(page.content)
        words = tree.xpath('//p/text()')
        main_story = [x.encode('ascii', 'ignore').encode('utf-8') for x in words if len(x) > 100]
        name = re.findall('\w*', urlparse(link).netloc)[2]
        file_output(name, main_story)
        print '%s has been processed' % link
    except:
        pass

def file_output(source, content_list):
    file_name = '%s.txt' % name
    file_new = open(file_name, 'w')
    file_new.write(name)
    file_new.write('\n')
    file_new.close()
    file_append = open(file_name, 'a')
    for x in content_list: file_append.write(x) 
    file_append.close()

main()
