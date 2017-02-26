#Dragon Source
*Your fake and biased news evaluator*

##Introduction
With the growing concern over the veracity and bias of news sources, as well at the rise of "fake news" accusations, we decide that we wanted to create something to programmatically show the trustworthiness of a website.
Using Markov chains and machine learning, we've created a program that reads a news story and evaluates it based on past data in an attempt to categorize the article's veracity and political leanings.

Using a combination of web scraping and the NewsAPI, we gathered data from sources across the political spectrum, including The New York Times, Bloomberg, and Breitbart News. The more similar the chains for an article were to
one of these sources, the more we classified them as left, center, or right, respectively.

*Disclaimer: This program is not meant to forward any political agenda. It is simply a statistical analysis of the word usage of news media. The members on our team range across the political spectrum.

##Languages
	* Python - for data gathering
	* C# - for Markov chains and evaluation

## Contributing
1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request!

Powered by NewsAPI.org (https://newsapi.org/)