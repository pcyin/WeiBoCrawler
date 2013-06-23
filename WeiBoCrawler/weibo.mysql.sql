create table WeiboList(
	id int not null primary key auto_increment,
	rid varchar(100),
	url varchar(100) not null,
	uid varchar(100),
	wid varchar(100),
	content varchar(500),
	has_img bit(1),
	com_finish bit(1)
);

create table WeiBoCommentList(
	id int not null primary key auto_increment,
	wid varchar(100) not null,
	uid varchar(100) not null,
	content varchar(500),
	cuid varchar(100) not null
);

alter table WeiboList modify uid  